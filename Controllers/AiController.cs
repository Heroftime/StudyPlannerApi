using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using StudyPlannerApi.Data;
using StudyPlannerApi.Models;

namespace StudyPlannerApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Kernel _kernel;

        public AiController(ApplicationDbContext context, Kernel kernel)
        {
            _context = context;
            _kernel = kernel;
        }

        // POST: api/Ai/GenerateCourseDescription
        [HttpPost("GenerateCourseDescription")]
        public async Task<ActionResult<string>> GenerateCourseDescription([FromBody] CourseDescriptionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CourseName))
            {
                return BadRequest("Course name is required.");
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            var prompt = $"Generate a detailed course description for a course named '{request.CourseName}'";
            if (!string.IsNullOrWhiteSpace(request.Code))
            {
                prompt += $" with code '{request.Code}'";
            }
            if (!string.IsNullOrWhiteSpace(request.Semester))
            {
                prompt += $" typically offered in {request.Semester}";
            }
            prompt += ". The description should be professional, informative, and around 2-3 sentences.";

            chatHistory.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);

            return Ok(new { description = response.Content });
        }

        // POST: api/Ai/AnalyzeCourse/{id}
        [HttpPost("AnalyzeCourse/{id}")]
        public async Task<ActionResult<string>> AnalyzeCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            var prompt = $"Analyze this course and provide insights:\n" +
                        $"Name: {course.Name}\n" +
                        $"Code: {course.Code ?? "N/A"}\n" +
                        $"Description: {course.Description ?? "N/A"}\n" +
                        $"Semester: {course.Semester ?? "N/A"}\n" +
                        $"Credit Hours: {course.CreditHours}\n\n" +
                        $"Provide a brief analysis including difficulty level, recommended prerequisites, and study tips.";

            chatHistory.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);

            return Ok(new
            {
                courseId = course.Id,
                courseName = course.Name,
                analysis = response.Content
            });
        }

        // POST: api/Ollama/SuggestStudyPlan
        [HttpPost("SuggestStudyPlan")]
        public async Task<ActionResult<string>> SuggestStudyPlan([FromBody] StudyPlanRequest request)
        {
            if (request.CourseIds == null || !request.CourseIds.Any())
            {
                return BadRequest("At least one course ID is required.");
            }

            var courses = await _context.Courses
                .Where(c => request.CourseIds.Contains(c.Id))
                .ToListAsync();

            if (!courses.Any())
            {
                return NotFound("No courses found with the provided IDs.");
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            var courseList = string.Join("\n", courses.Select(c =>
                $"- {c.Name} ({c.Code ?? "No Code"}): {c.CreditHours} credit hours, {c.Semester ?? "Unspecified semester"}"));

            var prompt = $"Create a study plan for the following courses:\n{courseList}\n\n" +
                        $"Total Credit Hours: {courses.Sum(c => c.CreditHours)}\n" +
                        $"Weeks Available: {request.WeeksAvailable}\n\n" +
                        $"Provide a structured study plan with time allocation and priorities.";

            chatHistory.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);

            return Ok(new
            {
                totalCourses = courses.Count,
                totalCreditHours = courses.Sum(c => c.CreditHours),
                studyPlan = response.Content
            });
        }

        // POST: api/Ai/GenerateTimePlanner
        [HttpPost("GenerateTimePlanner")]
        public async Task<ActionResult<string>> GenerateTimePlanner([FromBody] TimePlannerRequest request)
        {
            if (request.SubjectTimeData == null || !request.SubjectTimeData.Any())
            {
                return BadRequest("At least one subject with time data is required.");
            }

            var subjectIds = request.SubjectTimeData.Select(s => s.SubjectId).ToList();
            var subjects = await _context.Subjects
                .Where(s => subjectIds.Contains(s.Id))
                .ToListAsync();

            if (!subjects.Any()) // If there are no subjects in the database
            {
                return NotFound("No subjects found with the provided IDs.");
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            var subjectDetails = request.SubjectTimeData.Select(std =>
            {
                var subject = subjects.FirstOrDefault(s => s.Id == std.SubjectId);
                var avgTime = std.AverageTimeInMinutes ?? subject?.AverageTimeInMinutes ?? 60;
                return new
                {
                    Name = subject?.Name ?? "Unknown Subject",
                    TimeInMinutes = avgTime,
                    Description = subject?.Description
                };
            }).ToList();

            var subjectList = string.Join("\n", subjectDetails.Select(s =>
                $"- {s.Name}: {s.TimeInMinutes} minutes per session" + 
                (string.IsNullOrWhiteSpace(s.Description) ? "" : $" ({s.Description})")));

            var totalTimePerDay = subjectDetails.Sum(s => s.TimeInMinutes);
            var hoursAvailable = request.HoursAvailablePerDay ?? 8;
            var daysPerWeek = request.DaysPerWeek ?? 5;

            // Handle assignments - limit to 10 if more are provided
            var assignments = request.Assignments?.Take(10).ToList() ?? new List<string>();
            var assignmentSection = "";
            if (assignments.Any())
            {
                assignmentSection = $"\n\nUpcoming Assignments to schedule:\n" + 
                                   string.Join("\n", assignments.Select((a, index) => $"{index + 1}. {a}"));
            }

            var prompt = $"Create a personalized study planner based on the following subject time requirements:\n\n" +
                        $"{subjectList}\n\n" +
                        $"Total study time needed per cycle: {totalTimePerDay} minutes ({totalTimePerDay / 60.0:F1} hours)\n" +
                        $"Available hours per day: {hoursAvailable}\n" +
                        $"Days per week: {daysPerWeek}\n" +
                        $"Planning period: {request.WeeksToSchedule ?? 4} weeks" +
                        $"{assignmentSection}\n\n" +
                        $"Generate a realistic and balanced study schedule that:\n" +
                        $"1. Distributes study time effectively across all subjects\n" +
                        $"2. Accounts for the time each subject requires\n" +
                        $"3. Includes breaks and prevents burnout\n" +
                        $"4. Suggests optimal times for different subjects based on complexity\n" +
                        $"5. Provides daily and weekly study breakdowns" +
                        (assignments.Any() ? "\n6. References and schedules time for the specific assignments listed above\n" : "\n") +
                        $"\n?????????????????????????????????\n" +
                        $"CRITICAL FORMATTING REQUIREMENTS - YOU MUST FOLLOW THESE:\n" +
                        $"?????????????????????????????????\n\n" +
                        $"1. ABSOLUTELY NO MARKDOWN: Do not use ** for bold, do not use ## for headers, do not use * for lists, do not use ``` for code blocks, do not use | for tables\n" +
                        $"2. Use PLAIN TEXT ONLY with emojis for visual appeal\n" +
                        $"3. For section separators, use this exact line: ?????????????????????????????????\n" +
                        $"4. Use emojis GENEROUSLY throughout (at least 20-30 emojis total):\n" +
                        $"   - For subjects: ?? ?? ?? ?? ?? ?? ?? ?? ?? ??\n" +
                        $"   - For time/schedule: ? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ???\n" +
                        $"   - For breaks/rest: ? ?? ?? ??? ?? ?? ?? ??\n" +
                        $"   - For activities: ?? ?? ????? ?? ? ?? ?? ?? ??\n" +
                        $"   - For days: ?? ??? ?? ?\n" +
                        $"5. Structure each day like this example:\n\n" +
                        $"?????????????????????????????????\n" +
                        $"?? MONDAY - Week 1\n" +
                        $"?????????????????????????????????\n" +
                        $"? 9:00 AM - 10:30 AM ? ?? Computer Science (90 min)\n" +
                        $"? 10:30 AM - 10:45 AM ? ? Quick Break (15 min)\n" +
                        $"? 10:45 AM - 12:15 PM ? ?? Subject Name (90 min)\n\n" +
                        $"6. Make it colorful, engaging, and FUN to read!\n" +
                        $"7. Keep all text clean and readable - no asterisks, no pound signs, no special markdown symbols\n" +
                        $"8. Use simple dashes or arrows (?) instead of complex tables";

            chatHistory.AddUserMessage(prompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);

            return Ok(new
            {
                totalSubjects = subjects.Count,
                totalTimeRequired = totalTimePerDay,
                averageTimePerSubject = totalTimePerDay / Math.Max(1, subjectDetails.Count),
                hoursAvailablePerDay = hoursAvailable,
                daysPerWeek = daysPerWeek,
                totalAssignments = assignments.Count,
                studyPlanner = response.Content
            });
        }

        // POST: api/Ai/Chat
        [HttpPost("Chat")]
        public async Task<ActionResult<string>> Chat([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message is required.");
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage("You are a helpful study planner assistant. Help students with course planning, study strategies, and academic advice. Don't answer any other questions.");
            chatHistory.AddUserMessage(request.Message);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);

            return Ok(new
            {
                userMessage = request.Message,
                assistantResponse = response.Content
            });
        }

        // GET: api/Ai/Status
        [HttpGet("Status")]
        public async Task<ActionResult<object>> GetStatus()
        {
            try
            {
                var chatService = _kernel.GetRequiredService<IChatCompletionService>();
                var chatHistory = new ChatHistory();
                chatHistory.AddUserMessage("Say 'OK' if you're working.");

                var response = await chatService.GetChatMessageContentAsync(chatHistory);

                return Ok(new
                {
                    status = "Connected",
                    provider = "OpenAI",
                    model = "gpt-4o-mini",
                    message = "ChatGPT connection is active",
                    testResponse = response.Content
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    message = "Failed to connect to ChatGPT API",
                    error = ex.Message
                });
            }
        }
    }

    // Request models
    public class CourseDescriptionRequest
    {
        public string CourseName { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Semester { get; set; }
    }

    public class StudyPlanRequest
    {
        public List<int> CourseIds { get; set; } = new List<int>();
        public int WeeksAvailable { get; set; } = 15;
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }

    public class TimePlannerRequest
    {
        public List<SubjectTimeData> SubjectTimeData { get; set; } = new List<SubjectTimeData>();
        public int? HoursAvailablePerDay { get; set; } = 8;
        public int? DaysPerWeek { get; set; } = 5;
        public int? WeeksToSchedule { get; set; } = 4;
        /// <summary>
        /// Optional list of assignment names to include in the planner. Limited to 10 assignments.
        /// </summary>
        public List<string>? Assignments { get; set; }
    }

    public class SubjectTimeData
    {
        public int SubjectId { get; set; }
        /// <summary>
        /// Override the subject's default average time if provided
        /// </summary>
        public int? AverageTimeInMinutes { get; set; }
    }
}
