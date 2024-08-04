using Bogus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using UsersBackend.Data;
using UsersBackend.Models.Entities;

namespace UsersBackend.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public UsersController(ApplicationDBContext context)
        {
            _context = context;
        }

        //User Generation
        [HttpGet("generate")]
        public IActionResult GenerateUsers(int count)
        {
            var faker = new Faker<User>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.BirthDate, f => f.Date.Past(30))
                .RuleFor(u => u.City, f => f.Address.City())
                .RuleFor(u => u.Country, f => f.Address.CountryCode())
                .RuleFor(u => u.Avatar, f => f.Internet.Avatar())
                .RuleFor(u => u.Company, f => f.Company.CompanyName())
                .RuleFor(u => u.JobPosition, f => f.Name.JobTitle())
                .RuleFor(u => u.Mobile, f => f.Phone.PhoneNumber())
                .RuleFor(u => u.Username, f => f.Internet.UserName())
                .RuleFor(u => u.Email, f => f.Internet.Email())
                .RuleFor(u => u.Password, f => f.Internet.Password(length: 6))
                .RuleFor(u => u.Role, f => f.PickRandom(new[] { "admin", "user" }));

            var users = faker.Generate(count);

            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var fileName = "users.json";
            var contentType = "application/json";
            var fileContent = new System.Text.UTF8Encoding().GetBytes(json);

            return File(fileContent, contentType, fileName);
        }


        //Upload User File and Create Users in the Database
        [HttpPost("batch")]
        public async Task<IActionResult> UploadUsers([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty or doesn't exist");

            List<User> users;
            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                var content = await stream.ReadToEndAsync();
                users = JsonSerializer.Deserialize<List<User>>(content);
            }
            catch (JsonException)
            {
                return BadRequest("The uploaded file is not a valid JSON.");
            }

            if (users == null || users.Count == 0)
            {
                return BadRequest("No valid user data found in the uploaded file.");
            }

            int totalRecords = users.Count;
            int successfullyImported = 0;
            int notImported = 0;

            foreach (var user in users)
            {
                if (_context.Users.Any(u => u.Email == user.Email || u.Username == user.Username))
                {
                    notImported++;
                    continue;
                }

                user.Password = AuthController.EncodePassword(user.Password);

                _context.Users.Add(user);
                successfullyImported++;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                totalRecords = totalRecords,
                successfullyImported = successfullyImported,
                notImported = notImported
            });
        }


        //View My Profile
        [Authorize]
        [HttpGet("me")]
        public IActionResult ViewMyProfile()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("Invalid token.");
            }

            var userId = int.Parse(userIdClaim.Value);

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }


        //View Any User Profile
        [Authorize(Roles = "admin")]
        [HttpGet("{username}")]
        public IActionResult ViewUserProfile(string username)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            return Ok(user);
        }
    }
}
