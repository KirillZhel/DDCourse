using Api.Models.Attach;
using Api.Services;
using Common.Consts;
using Common.Extentions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net.WebSockets;

namespace Api.Controllers
{
    [Route("api/[controller]/[action]")]
	[ApiController]
	public class AttachController : ControllerBase
	{
        private readonly PostService _postService;
		private readonly UserService _userService;

		public AttachController(PostService postService, UserService userService)
        {
            _postService = postService;
            _userService = userService;
        }

        [HttpPost]
		public async Task<List<MetadataModel>> UploadFiles([FromForm] List<IFormFile> files)
		{
			var res = new List<MetadataModel>();
			foreach (var file in files)
			{
				res.Add(await UploadFile(file));
			}
			return res;
		}

        [HttpGet]
        [Route("{postContentId}")]
        public async Task<FileStreamResult> GetPostContent(Guid postContentId, bool download = false)
		    => RenderAttach(await _postService.GetPostContent(postContentId), download);

        [HttpGet]
        [Route("{postId}")]
        public async Task<FileStreamResult> GetUserAvatar(Guid userId, bool download = false)
            => RenderAttach(await _userService.GetUserAvatar(userId), download);

        [HttpGet]
		public async Task<FileStreamResult> GetCurentUserAvatar(bool download = false)
            => await GetUserAvatar(User.GetClaimValue<Guid>(ClaimNames.Id), download);

        private async Task<MetadataModel> UploadFile(IFormFile file)
        {
            var tempPath = Path.GetTempPath(); //папка временных файлов
            var meta = new MetadataModel
            {
                TempId = Guid.NewGuid(),
                Name = file.FileName,
                MimeType = file.ContentType,
                Size = file.Length
            };

            var newPath = Path.Combine(tempPath, meta.TempId.ToString());
            var fileInfo = new FileInfo(newPath);

            if (fileInfo.Exists)
            {
                throw new Exception("file exist");
            }
            else
            {
                if (fileInfo.Directory == null)
                {
                    throw new Exception("temp is null");
                }
                else
                {
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory?.Create();
                    }
                }

                using (var stream = System.IO.File.Create(newPath))
                {
                    await file.CopyToAsync(stream);
                }

                return meta;
            }
        }

        private FileStreamResult RenderAttach(AttachModel attach, bool download)
        {
            // проверить, что файл есть
            // сделать FileInfo(attach.FilePath + name)
            // накидать проверок (fileInfo.Exists и тп)
            // проверить MimeType на null (у некоторых файлов сложных тип файла придётся узнавать по его хедеру(по первым байтам файла))

            var fileStream = new FileStream(attach.FilePath, FileMode.Open);
            var ext = Path.GetExtension(attach.Name);
            if (download)
            {
                return File(fileStream, attach.MimeType, $"{attach.Id}{ext}");
            }
            else
            {
                return File(fileStream, attach.MimeType);
            }
        }
    }
}
