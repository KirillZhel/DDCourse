using Api.Configs;
using Api.Models.Attach;
using Api.Models.Post;
using Api.Models.User;
using AutoMapper;
using DAL;
using DAL.Entities;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Api.Services
{
    public class PostService
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private Func<PostContent, string?>? _linkContentGenerator;
        private Func<User, string?>? _linkAvatarGenerator;

        public PostService(IMapper mapper, DataContext context)
        {
            _mapper = mapper;
            _context = context;
        }

        public void SetLinkGenerator(
            Func<PostContent, string?> linkContentGenerator,
            Func<User, string?> linkAvatarGenerator)
        {
            _linkContentGenerator = linkContentGenerator;
            _linkAvatarGenerator = linkAvatarGenerator;
        }

        public async Task CreatePost(CreatePostRequest request)
        {
            var model = _mapper.Map<CreatePostModel>(request);

            model.Contents.ForEach(x =>
            {
                x.AuthorId = model.AuthorId;
                x.FilePath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "Attaches",
                    x.TempId.ToString());

                var tempFile = new FileInfo(Path.Combine(Path.GetTempPath(), x.TempId.ToString()));
                if (tempFile.Exists)
                {
                    var destFileInfo = new FileInfo(x.FilePath);
                    if (destFileInfo.Directory != null && !destFileInfo.Directory.Exists)
                        destFileInfo.Directory.Create();

                    File.Move(tempFile.FullName, x.FilePath, true);
                }
            });

            var dbModel = _mapper.Map<Post>(model);
            await _context.Posts.AddAsync(dbModel);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PostModel>> GetPosts(int take, int skip)
        {
            var posts = await _context.Posts
                .Include(x => x.Author)
                .ThenInclude(x => x.Avatar)
                .Include(x => x.PostContents)
                .AsNoTracking() // AsNoTracking - ?
                .OrderByDescending(x => x.Created)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            
            var res = posts.Select(post => new PostModel
            {
                Author = _mapper.Map<User, UserAvatarModel>(post.Author, o => o.AfterMap(FixAvatar)),
                Description = post.Description,
                Id = post.Id,
                Contents = post.PostContents?.Select(x =>
                    _mapper.Map<PostContent, AttachExternalModel>(x, o => o.AfterMap(FixContent))).ToList()
            }).ToList();

            return res;
        }

        public async Task<AttachModel> GetPostContent(Guid postContentId)
        {
            var res = await _context.PostContents.FirstOrDefaultAsync(x => x.Id == postContentId);

            return _mapper.Map<AttachModel>(res);
        }

        private void FixAvatar(User s, UserAvatarModel d)
        {
            d.AvatarLink = s.Avatar == null ? null : _linkAvatarGenerator?.Invoke(s);
        }

        private void FixContent(PostContent s, AttachExternalModel d)
        {
            d.ContentLink = _linkContentGenerator?.Invoke(s);
        }
    }
}
