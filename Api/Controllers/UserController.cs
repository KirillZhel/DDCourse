using Api.Models;
using Api.Services;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using DAL;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers
{
	[Route("api/[controller]/[action]")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly UserService _userService;

		public UserController(UserService userService)
		{
			_userService = userService;
		}

		[HttpPost]
		public async Task CreateUser(CreateUserModel model)
		{
			if (await _userService.CheckUserExist(model.Email))
			{
				throw new Exception("user is exist");
			}
				await _userService.CreateUser(model);
		}

		[HttpPost]
		[Authorize]
		public async Task AddAvatarToUser(MetadataModel model)
		{
			var userIdString = User.Claims.FirstOrDefault(u => u.Type == "id")?.Value;

			if (Guid.TryParse(userIdString, out var userId))
			{
				var tempFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), model.TempId.ToString()));

				if (!tempFileInfo.Exists)
					throw new Exception("file not found");
				else
				{
					var path = Path.Combine(Directory.GetCurrentDirectory(), "Attaches", model.TempId.ToString());
					var destFileInfo = new FileInfo(path);
					if (destFileInfo.Directory != null && !destFileInfo.Directory.Exists)
						destFileInfo.Directory.Create();

					System.IO.File.Copy(tempFileInfo.FullName, path, true); // переносим файл из временной папки в постоянную

					await _userService.AddAvatarToUser(userId, model, path);
				}
			}
			else
				throw new Exception("you are not authorized");
		}

		// не "боевой" метод 
		[HttpGet]
		public async Task<FileResult> GetUserAvatar(Guid userId)
		{
			var attach = await _userService.GetUserAvatar(userId);
			// проверить, что файл есть
			// сделать FileInfo(attach.FilePath + name)
			// накидать проверок (fileInfo.Exists и тп)
			// проверить MimeType на null (у некоторых файлов сложных тип файла придётся узнавать по его хедеру(по первым байтам файла))

			return File(System.IO.File.ReadAllBytes(attach.FilePath), attach.MimeType);
		}

		[HttpGet]
		public async Task<FileResult> DownloadAvatar(Guid userId)
		{
			var attach = await _userService.GetUserAvatar(userId);

			HttpContext.Response.ContentType = attach.MimeType;
			FileContentResult result = new FileContentResult(System.IO.File.ReadAllBytes(attach.FilePath), attach.MimeType)
			{
				FileDownloadName = attach.Name
			};

			return result;
		}

		[HttpGet]
		[Authorize]
		public async Task<List<UserModel>> GetUsers() => await _userService.GetUsers();

		[HttpGet]
		[Authorize]
		public async Task<UserModel> GetCurrentUser()
		{
			var userIdString = User.Claims.FirstOrDefault(u => u.Type == "id")?.Value;

			if(Guid.TryParse(userIdString, out var userId))
				return await _userService.GetUser(userId); 
			else
				throw new Exception("you are not authorized");
		}
	}
}
