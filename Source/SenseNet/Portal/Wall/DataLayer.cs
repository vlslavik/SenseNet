﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.Storage;
using SenseNet.Search;
using SenseNet.ContentRepository;
using SenseNet.Portal.Workspaces;
using SenseNet.ContentRepository.Workspaces;
using SenseNet.ContentRepository.Storage.Security;

namespace SenseNet.Portal.Wall
{
    public class DataLayer
    {
        // ================================================================================================ Helper classes
        private class CommentsLikesComparer : IEqualityComparer<PostInfo>
        {
            public bool Equals(PostInfo x, PostInfo y)
            {
                return x.Type == y.Type && x.SharedContent.Id == y.SharedContent.Id;
            }

            public int GetHashCode(PostInfo obj)
            {
                return obj.SharedContent.Id.GetHashCode();
            }
        }


        // ================================================================================================ Public methods
        /// <summary>
        /// Retrieves all posts under given contextPath.
        /// </summary>
        /// <param name="contextPath">Path of context, ie. workspace.</param>
        /// <returns></returns>
        public static IEnumerable<PostInfo> GetPostsForWorkspace(string contextPath)
        {
            var settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled, Sort = new SortInfo[] { new SortInfo { FieldName = "CreationDate", Reverse = true } } };
            var posts = ContentQuery.Query(ContentRepository.SafeQueries.InTreeAndTypeIs, settings,
                contextPath, "Post").Nodes.Select(n => new PostInfo(n));
            
            // gather all journalid-s: these are ids to which post has already been created in the Content Repository
            var journalIds = posts.Select(p => p.JournalId).Distinct();

            #region Gather journal posts: removed 
            //var journalItems = Journals.GetItemsForWorkspace(contextPath);

            //// get last paths of journals. Get all journals grouped by nodeids
            //var lastPaths = (from j in journalItems
            //                 group j by j.NodeId into grp
            //                 select grp.First()).ToDictionary(j => j.NodeId, j => j.Wherewith);

            //// gather crudposts, where createdby is a valid user (not SYSTEMUSER)
            //// and it has not been persisted to the content repository yet (journalid is not contained in journalids above)
            //var crudPosts = journalItems.Select(j => new PostInfo(j, lastPaths[j.NodeId])).Where(p => p.CreatedBy != null && !journalIds.Contains(p.JournalId)); 
            #endregion

            // gather likes and comments corresponding to content under this workspace
            var postsFolderPath = RepositoryPath.Combine(contextPath, "Posts");
            var contentComments = ContentQuery.Query("+InTree:@0 +(Type:Comment Type:Like) -InTree:@1", settings,
                contextPath, postsFolderPath).Nodes.Select(n => PostInfo.CreateFromCommentOrLike(n)).Where(p => p != null).Distinct(new CommentsLikesComparer());

            return posts.Union(contentComments).OrderByDescending(p => p.CreationDate);
        }
        public static IEnumerable<PostInfo> GetPostsForUser(User user, string profilePath)
        {
            // get all wall container workspaces - instead of loading the content of the journal and individually determine the wall container workspace
            //var wallWorkspacesPaths = ContentQuery.Query("+TypeIs:Workspace +IsWallContainer:1").Nodes.Select(n => n.Path);

            // gather users's posts:
            // - gather all journal posts from everywhere, that was created by the user
            // - gather all big posts that were put out to his/her wall
            var settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled, Sort = new SortInfo[] { new SortInfo { FieldName = "CreationDate", Reverse = true } } };
            var posts = ContentQuery.Query("(+CreatedById:@0 +Type:Post -PostType:@1) (+InTree:@2 +Type:Post)", settings,
                 user.Id, (int)PostType.BigPost, profilePath).Nodes.Select(n => new PostInfo(n));
            
            #region Gather journal posts: removed 
            // gather all journalid-s: these are ids to which post has already been created in the Content Repository
            //var journalIds = posts.Select(p => p.JournalId).Distinct();
            
            //var journalItems = Journals.GetItemsForUser(user);

            //// get last paths of journals. Get all journals grouped by nodeids
            //var lastPaths = (from j in journalItems
            //                 group j by j.NodeId into grp
            //                 select grp.First()).ToDictionary(j => j.NodeId, j => j.Wherewith);

            //// gather users's activities
            //// gather crudposts, where createdby is current user
            //// and it has not been persisted to the content repository yet (journalid is not contained in journalids above)
            //var crudPosts = journalItems.Select(j => new PostInfo(j, lastPaths[j.NodeId])).Where(p => p.CreatedBy != null && !journalIds.Contains(p.JournalId)); 
            #endregion

            return posts.OrderByDescending(p => p.CreationDate);
        }
        public static IEnumerable<PostInfo> GetPostsForContent(Node content)
        {
            // share posts of current content
            var settings = new QuerySettings { Sort = new SortInfo[] { new SortInfo { FieldName = "CreationDate", Reverse = true } } };
            var posts = ContentQuery.Query("+SharedContent:@0 +Type:Post", settings, content.Id).Nodes.Select(n => new PostInfo(n));

            // gather all journalid-s: these are ids to which post has already been created in the Content Repository
            var journalIds = posts.Select(p => p.JournalId).Distinct();

            var journalItems = Journals.GetItemsForContent(content.Id);

            // get last paths of journals. Get all journals grouped by nodeids
            var lastPaths = (from j in journalItems
                             group j by j.NodeId into grp
                             select grp.First()).ToDictionary(j => j.NodeId, j => j.Wherewith);

            // gather crudposts, where createdby is a valid user (not SYSTEMUSER)
            // and it has not been persisted to the content repository yet (journalid is not contained in journalids above)
            var crudPosts = journalItems.Select(j => new PostInfo(j, lastPaths[j.NodeId])).Where(p => p.CreatedBy != null && !journalIds.Contains(p.JournalId));

            return posts.Union(crudPosts).OrderByDescending(p => p.CreationDate);
        }
        /// <summary>
        /// Retrieves comments for a given Post/Content
        /// </summary>
        /// <param name="postid">Id of Post.</param>
        /// <returns></returns>
        public static QueryResult GetComments(int parentId)
        {
            var parent = NodeHead.Get(parentId);
            if (parent == null)
                return null;
            var commentFolderPath = RepositoryPath.Combine(parent.Path, "Comments");

            var settings = new QuerySettings { EnableAutofilters = FilterStatus.Disabled, Sort = new SortInfo[] { new SortInfo { FieldName = "CreationDate", Reverse = false } } };
            var result = ContentQuery.Query("+InFolder:@0 +Type:Comment", settings, commentFolderPath);
            return result;
        }
        /// <summary>
        /// Retrieves likes for a given Content/Post/Comment
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static QueryResult GetLikes(int parentId)
        {
            var parent = NodeHead.Get(parentId);
            if (parent == null)
                return null;
            var result = ContentQuery.Query(ContentRepository.SafeQueries.InFolderAndTypeIs, new QuerySettings { EnableAutofilters = FilterStatus.Disabled },
                RepositoryPath.Combine(parent.Path, "Likes"), "Like");
            return result;
        }
        public static GenericContent CreateManualPost(string contextPath, string text)
        {
            return DataLayer.CreatePost(contextPath, text, 0, PostType.BigPost, null, null);
        }
        public static GenericContent CreateSharePost(string contextPath, string text, int sharedContentId)
        {
            var sharedContent = Node.LoadNode(sharedContentId);
            return DataLayer.CreatePost(contextPath, text, 0, PostType.BigPost, sharedContent, null);
        }
        /// <summary>
        /// Creates a new Post under the given contextpath
        /// </summary>
        /// <param name="actualContextPath">New posts from journal items will be created under contextPath</param>
        /// <param name="text"></param>
        /// <param name="creationDate"></param>
        /// <param name="journalId">The post may be automatically created from a journal item, in this case the journal id is passed</param>
        /// <returns></returns>
        public static GenericContent CreatePost(string contextPath, string text, int journalId, PostType type, Node sharedContent, string details)
        {
            var actualContextPath = contextPath;
            if (type != PostType.BigPost)
            {
                // for journals the context should always be its parent wall workspace. That's where the post will be saved.
                var ws = Workspace.GetWorkspaceWithWallForNode(sharedContent);
                if (ws != null)
                    actualContextPath = ws.Path;
            }
            var postsPath = RepositoryPath.Combine(actualContextPath, "Posts");
            var postsFolder = Node.LoadNode(postsPath) as GenericContent;
            if (postsFolder == null)
            {
                using (new SystemAccount())
                {
                    var context = Node.LoadNode(actualContextPath);
                    postsFolder = new GenericContent(context, "Posts");
                    postsFolder.Name = "Posts";
                    postsFolder.Hidden = true;
                    postsFolder.Save();
                }
            }
            var post = new GenericContent(postsFolder, "Post");
            post.Description = text;
            post["JournalId"] = journalId;
            post["PostType"] = type;
            post["PostDetails"] = details;
            if (sharedContent != null)
                post.SetReference("SharedContent", sharedContent);
            post.Save();
            return post;
        }
        /// <summary>
        /// Creates a new Comment for the given Post/Content
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="contextPath">New posts from journal items will be created under contextPath</param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static GenericContent CreateComment(string clientId, string contextPath, string text)
        {
            var post = GetPostFromId(clientId, contextPath);

            var commentsFolder = Node.LoadNode(RepositoryPath.Combine(post.Path, "Comments")) as GenericContent;
            if (commentsFolder == null)
            {
                using (new SystemAccount())
                {
                    commentsFolder = new SystemFolder(post) as GenericContent;
                    commentsFolder.Name = "Comments";
                    commentsFolder.Hidden = true;
                    commentsFolder.Save();
                }
            }

            var comment = new GenericContent(commentsFolder, "Comment");
            //var comment = new GenericContent(post, "Comment");
            comment.Description = text;
            comment.Save();
            return comment;
        }
        /// <summary>
        /// Creates a new Like for the given Post/Content/Comment
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="contextPath">New posts from journal items will be created under contextPath</param>
        /// <returns></returns>
        public static void CreateLike(string clientId, string contextPath, out int postId)
        {
            var likedContent = GetPostFromId(clientId, contextPath);

            var likesFolder = Node.LoadNode(RepositoryPath.Combine(likedContent.Path, "Likes")) as GenericContent;
            if (likesFolder == null)
            {
                using (new SystemAccount())
                {
                    likesFolder = new SystemFolder(likedContent) as GenericContent;
                    likesFolder.Name = "Likes";
                    likesFolder.Hidden = true;
                    likesFolder.Save();
                }
            }

            var likeContent = new GenericContent(likesFolder, "Like");
            likeContent.Save();

            // return postId, as it will be used for LikeList and LikeMarkup
            postId = likedContent.Id;
        }
        /// <summary>
        /// Deletes a Like from the given Post/Content/Comment
        /// </summary>
        /// <param name="itemId"></param>
        public static void DeleteLike(string clientId, out int postId)
        {
            // this method can only be called if the post already exists in the Content Repository
            // but itemId may still be referring to journalId, so let's load the corresponding content. Therefore string.Empty is enough here
            var post = GetPostFromId(clientId, string.Empty);

            var likeFolderPath = RepositoryPath.Combine(post.Path, "Likes");
            var likeContents = ContentQuery.Query("+InFolder:@0 +Type:Like +CreatedById:@1",
                new QuerySettings { EnableAutofilters = FilterStatus.Disabled },
                likeFolderPath, SenseNet.ContentRepository.User.Current.Id).Nodes;

            foreach (var likeContent in likeContents)
            {
                var likeGc = likeContent as GenericContent;
                if (likeGc != null)
                    likeGc.Delete(true);
            }

            // return postId, as it will be used for LikeList and LikeMarkup
            postId = post.Id;
        }


        // ================================================================================================ Private methods
        /// <summary>
        /// Get a post from clientId. If it is a manual post, it comes from repository. 
        /// If it is a journal post, it either comes from journal and a repository post is created, or is already persisted to repository.
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="contextPath">New posts from journal items will be created under contextPath</param>
        /// <returns></returns>
        private static Node GetPostFromId(string clientId, string contextPath)
        {
            Node post = null;
            var itemID = PostInfo.GetIdFromClientId(clientId);
            if (PostInfo.IsJournalId(clientId))
            {
                // CRUD post, create a manual post
                // only create it if it is not yet created!
                post = ContentQuery.Query("JournalId:@0", null, itemID).Nodes.FirstOrDefault();
                if (post == null)
                {
                    var item = Journals.GetSingleItem(itemID);
                    // lastpath is empty here: we wont use it from this scenario
                    var postInfo = new PostInfo(item, string.Empty);
                    var sharedContent = Node.LoadNode(item.NodeId);
                    post = CreatePost(contextPath, postInfo.Text, itemID, postInfo.Type, sharedContent, postInfo.Details);
                }
            }
            else
            {
                post = Node.LoadNode(itemID);
            }
            return post;
        }
    }
}
