using Common.Consts;
using Api.Models.Attach;
using Api.Models.User;
using Api.Services;
using Common.Extentions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [Route("api/[controller]/[action]")]
	[ApiController]
    [Authorize]
    public class UserController : ControllerBase
	{
		private readonly UserService _userService;

		public UserController(UserService userService)
		{
			_userService = userService;
			_userService.SetLinkGenerator(x =>
				Url.ControllerAction<AttachController>(
					nameof(AttachController.GetUserAvatar),
					new
						{
							userId = x.Id
						})
			);
		}

		//[HttpPost]
		//[Authorize]
		//public async Task CreatePost(CreatePostModel model)
		//{
		//	var userIdString = User.Claims.FirstOrDefault(u => u.Type == "id")?.Value;

		//	if (Guid.TryParse(userIdString, out var userId))
		//	{
		//		var tempFileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), model.TempId.ToString()));
		//		if (!tempFileInfo.Exists)
		//			throw new Exception("file not found");
		//		else
		//		{
		//			var path = "";
					
		//			await _userService.CreatePost(userId, model, path);
		//		}
		//	}
		//	else
		//		throw new Exception("you are not authorized");
		//}

		[HttpPost]
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
                    //tempFileInfo.Delete(); - по идее надо удалять временные файлы после
                    await _userService.AddAvatarToUser(userId, model, path);
				}
			}
			else
				throw new Exception("you are not authorized");
		}

		//[HttpGet]
		//public async Task<FileResult> DownloadAvatar(Guid userId)
		//{
		//	var attach = await _userService.GetUserAvatar(userId);

		//	HttpContext.Response.ContentType = attach.MimeType;
		//	FileContentResult result = new FileContentResult(System.IO.File.ReadAllBytes(attach.FilePath), attach.MimeType)
		//	{
		//		FileDownloadName = attach.Name
		//	};

		//	return result;
		//}

		[HttpGet]
		public async Task<IEnumerable<UserAvatarModel>> GetUsers()
			=> await _userService.GetUsers();

		[HttpGet]
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
