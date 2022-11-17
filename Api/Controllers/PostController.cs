﻿using Api.Models.Attach;
using Api.Models.Post;
using Api.Services;
using Common.Consts;
using Common.Extentions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Api.Controllers
{
	[Route("api/[controller]/[action]")]
	[ApiController]
	[Authorize]
	public class PostController : ControllerBase
	{
		private readonly PostService _postService;
		public PostController(PostService postService)
		{
			_postService = postService;

			_postService.SetLinkGenerator(
				linkAvatarGenerator: x =>
					Url.ControllerAction<AttachController>(
						nameof(AttachController.GetUserAvatar),
						new { userId = x.Id }),
				linkContentGenerator: x =>
					Url.ControllerAction<AttachController>(
						nameof(AttachController.GetPostContent),
						new { postContentId = x.Id }));
		}

		[HttpGet]
		public async Task<List<PostModel>> GetPosts(int skip = 0, int take = 10)
			=> await _postService.GetPosts(skip, take);

        [HttpPost]
        public async Task CreatePost(CreatePostRequest request)
        {
			if (!request.AuthorId.HasValue)
			{
                var userId = User.GetClaimValue<Guid>(ClaimNames.Id);
                if (userId == default)
                    throw new Exception("Not Authorize");

				request.AuthorId = userId;
            }

            await _postService.CreatePost(request);
        }
    }
}