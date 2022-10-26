using Api.Models;
using AutoMapper;
using DAL;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
	[Route("api/[controller]/[action]")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly IMapper _mapper;
		private readonly DAL.DataContext _context;

		public UserController(IMapper mapper, DataContext context)
		{
			_mapper = mapper;
			_context = context;
		}

		[HttpPost]
		public async Task CreateUser(CreateUserModel model)
		{
			//прописывать логику проверки модели здесь бессмысленно, так как это можно сделать в самой модели CreateUserModel
			var dbUser = _mapper.Map<DAL.Entities.User>(model);
		}
	}
}
