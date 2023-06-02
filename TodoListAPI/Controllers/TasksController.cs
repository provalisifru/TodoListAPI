using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoList.Data;
using TodoList.Models;

namespace TodoListAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]



    public class TasksController : ControllerBase
    {
        private readonly TodoListContext _context;

        public TasksController(TodoListContext context)
        {
            _context = context;
        }

        private Guid GetUserIdFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);

            var userIdClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == "UserId");

            if (userIdClaim != null)
            {
                string userIdString = userIdClaim.Value;
                if (Guid.TryParse(userIdString, out Guid userId))
                {
                    return userId;
                }
            }

            // Return a default value or throw an exception if the user ID is not found or not in the expected format
            throw new Exception("Failed to extract user ID from JWT token.");
        }

        // GET: api/Tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoList.Models.Task>>> GetTasks()
        {
            if (_context.Tasks == null)
            {
                return NotFound();
            }
            return await _context.Tasks.ToListAsync();
        }

        // GET: api/Tasks/5
        [HttpGet("userTasks")]
        public async Task<ActionResult<List<TodoList.Models.Task>>> GetTasksByUserId()
        {
            string? token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            Guid userId = GetUserIdFromToken(token!);
            var tasks = await _context.Tasks.Where(t => t.UserId == userId).ToListAsync();

            if (tasks.Count == 0)
            {
                return NotFound();
            }

            return tasks;
        }

        // PUT: api/Tasks/userTasks
        [HttpPatch("userTasks")]
        public async Task<IActionResult> PatchTasks([FromBody] List<TaskUpdateData> tasksToUpdate)
        {
            string? token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            Guid userId = GetUserIdFromToken(token!);

            foreach (var taskUpdateData in tasksToUpdate)
            {
                var existingTask = await _context.Tasks.FindAsync(taskUpdateData.TaskId);

                if (existingTask == null || existingTask.UserId != userId)
                {
                    return NotFound();
                }

                existingTask.IsCompleted = taskUpdateData.IsCompleted;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Failed to update the tasks.");
            }

            return NoContent();
        }

        public class TaskUpdateData
        {
            public Guid TaskId { get; set; }
            public sbyte IsCompleted { get; set; }
        }


        // POST: api/Tasks
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("userTasks")]
        public async Task<ActionResult<TodoList.Models.Task>> PostTask([FromBody] TodoList.Models.Task task)
        {
            // Retrieve the token from the request headers
            string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Validate and extract the user ID from the token
            Guid userId = GetUserIdFromToken(token);

            try
            {
                task.TaskId = Guid.NewGuid();
                task.UserId = userId;

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetTasksByUserId", new { }, task);
            }

            catch (DbUpdateException)
            {
                if (TaskExists(task.TaskId))
                {
                    return Conflict("A task with the same ID already exists.");
                }
                else
                {
                    return StatusCode(500, "An error occurred while saving the task.");
                }
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while processing the request.");
            }
        }

        // DELETE: api/Tasks
        [HttpDelete("userTasks")]
        public async Task<IActionResult> DeleteTasks([FromBody] List<Guid> taskIds)
        {
            string? token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            Guid userId = GetUserIdFromToken(token!);

            var tasksToDelete = await _context.Tasks
                .Where(task => taskIds.Contains(task.TaskId) && task.UserId == userId)
                .ToListAsync();

            if (tasksToDelete.Count == 0)
            {
                return NotFound();
            }

            _context.Tasks.RemoveRange(tasksToDelete);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Failed to delete the tasks.");
            }

            return NoContent();
        }

        [HttpDelete("deleteAllTasks")]
        public async Task<IActionResult> DeleteTasks()
        {
            string? token = Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");

            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized();
            }

            Guid userId = GetUserIdFromToken(token!);

            var tasksToDelete = await _context.Tasks
                .Where(task => task.UserId == userId)
                .ToListAsync();

            if (tasksToDelete.Count == 0)
            {
                return NotFound();
            }

            _context.Tasks.RemoveRange(tasksToDelete);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(500, "Failed to delete the tasks.");
            }

            return NoContent();
        }

        private bool TaskExists(Guid id)
        {
            return (_context.Tasks?.Any(e => e.TaskId == id)).GetValueOrDefault();
        }
    }
}
