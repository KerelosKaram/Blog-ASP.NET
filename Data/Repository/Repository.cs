﻿using Blog.Helpers;
using Blog.Models;
using Blog.Models.Comments;
using Blog.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Blog.Data.Repository
{
    public class Repository : IRepository
    {
        private AppDbContext _ctx;

        public Repository(AppDbContext ctx)
        {
            _ctx = ctx;
        }

        public void AddPost(Post post)
        {
            // Save the post to the database using the DbContext
            _ctx.Posts.Add(post);
        }

        public List<Post> GetAllPosts()
        {
            return _ctx.Posts
                .ToList();
        }

        public IndexViewModel GetAllPosts(int pageNumber, string category, string search)
        {
            Func<Post, bool> InCategory = (post) => { return post.Category.ToLower().Equals(category.ToLower()); };

            int pageSize = 5;
            int skipAmount = pageSize * (pageNumber - 1);

            var query = _ctx.Posts.AsNoTracking().AsQueryable();
            //_ctx.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

            if (!String.IsNullOrEmpty(category))
            {
                query = query.AsEnumerable().Where(x => InCategory(x)).AsQueryable();
            }

            if (!String.IsNullOrEmpty(search))
            {
                //query = query.Where(x => x.Title.Contains(search) || x.Body.Contains(search) || x.Description.Contains(search));
                query = query.Where(x => EF.Functions.Like(x.Title, $"%{search}%")
                                      || EF.Functions.Like(x.Body, $"%{search}%")
                                      || EF.Functions.Like(x.Category, $"%{search}%")
                                      || EF.Functions.Like(x.Description, $"%{search}%"));
            }

            int postsCount = query.Count();
            int pageCount = (int)Math.Ceiling((double)postsCount / pageSize);

            return new IndexViewModel
            {
                PageNumber = pageNumber,
                PageCount = pageCount,
                NextPage = postsCount > skipAmount + pageSize,
                Pages = PageHelper.PageNumbers(pageNumber, pageCount).ToList(),
                Category = category,
                Search = search,
                Posts = query
                .Skip(skipAmount)
                .Take(pageSize)
                .ToList()
            };

        }

       

        public Post GetPost(int id)
        {
            return _ctx.Posts
                .Include(p => p.MainComments)
                .ThenInclude(mc => mc.SubComments)
                .FirstOrDefault(p => p.Id == id);
        }

        public void RemovePost(int id)
        {
            _ctx.Remove(GetPost(id));
        }


        public void UpdatePost(Post post)
        {
            _ctx.Posts.Update(post);
        }

        public async Task<bool> SaveChangesAsync()
        {
            if (await _ctx.SaveChangesAsync() > 0)
            {
                return true;
            }
            return false;
        }

        public void AddSubComment(SubComment comment)
        {
            _ctx.SubComments.Add(comment);
        }
    }
}
