using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Ollama;
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

        // POST: api/Ollama/GenerateCourseDescription
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

        // POST: api/Ollama/AnalyzeCourse/{id}
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


        [HttpPost("PoeticCourse/{id}")]
        public async Task<ActionResult<string>> PoeticCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);

            if (course == null)
            {
                return NotFound();
            }

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();

            var prompt = $"Can you write me a short poem for the course subject {course.Name}";
                   

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

        // POST: api/Ollama/Chat
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

        // GET: api/Ollama/Status
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
                    endpoint = "http://localhost:11434",
                    model = "llama3.2",
                    message = "Ollama connection is active"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = "Error",
                    message = "Failed to connect to Ollama",
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
}
