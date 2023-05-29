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

        // PUT: api/Tasks/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTask(Guid id, TodoList.Models.Task task)
        {
            if (id != task.TaskId)
            {
                return BadRequest();
            }

            _context.Entry(task).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TaskExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Tasks
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TodoList.Models.Task>> PostTask(TodoList.Models.Task task)
        {
            // Retrieve the token from the request headers
            string token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Validate and extract the user ID from the token
            Guid userId = GetUserIdFromToken(token);

            if (_context.Tasks == null)
            {
                return Problem("Entity set 'TodoListContext.Tasks' is null.");
            }

            task.TaskId = Guid.NewGuid();
            task.UserId = userId;
            _context.Tasks.Add(task);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (TaskExists(task.TaskId))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtAction("GetTask", new { id = task.TaskId }, task);
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            if (_context.Tasks == null)
            {
                return NotFound();
            }
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TaskExists(Guid id)
        {
            return (_context.Tasks?.Any(e => e.TaskId == id)).GetValueOrDefault();
        }
    }
}
