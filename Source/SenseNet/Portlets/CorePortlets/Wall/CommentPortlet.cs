using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Portal.UI.PortletFramework;
using SenseNet.Search;
using SenseNet.ContentRepository;
using SenseNet.Portal.Wall;
using System.Web.UI.WebControls;
using SenseNet.Portal.UI;
using SenseNet.Portal.UI.Controls;
using SenseNet.ContentRepository.Storage.Schema;
using SenseNet.ContentRepository.Storage;
using SenseNet.Diagnostics;

namespace SenseNet.Portal.Portlets.Wall
{
    public class CommentPortlet : ContextBoundPortlet
    {
        // ================================================================================================ Constructor
        public CommentPortlet()
        {
            this.Name = "$CommentPortlet:PortletDisplayName";
            this.Description = "$CommentPortlet:PortletDescription";
            this.Category = new PortletCategory(PortletCategoryType.Enterprise20);
        }


        // ================================================================================================ Methods
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            
            if (ShowExecutionTime)
                Timer.Start();
            
            UITools.AddScript("$skin/scripts/sn/SN.Wall.js");

            if (ShowExecutionTime)
                Timer.Stop();
        }
        protected override void CreateChildControls()
        {
            if (this.ContextNode == null)
                return;

            if (ShowExecutionTime)
                Timer.Start();

            // gather posts for this content
            List<PostInfo> posts;
            using (new OperationTrace("Wall - Gather posts"))
            {
                posts = DataLayer.GetPostsForContent(this.ContextNode).ToList();
            }
            string postsMarkup;
            using (new OperationTrace("Wall - Posts markup"))
            {
                postsMarkup = WallHelper.GetWallPostsMarkup(this.ContextNode.Path, posts);
            }

            CommentInfo contentCommentInfo;
            LikeInfo contentLikeInfo;
            using (new OperationTrace("Wall - Gather content comments"))
            {
                var settings = new QuerySettings() { EnableAutofilters = FilterStatus.Disabled };
                var allCommentsAndLikes = ContentQuery.Query(ContentRepository.SafeQueries.InTreeAndTypeIs, settings,
                    this.ContextNode.Path, new[] { "Comment", "Like" }).Nodes.ToList();

                var commentNodeTypeId = NodeType.GetByName("Comment").Id;
                var likeTypeId = NodeType.GetByName("Like").Id;

                var commentsForPost = allCommentsAndLikes.Where(c => c.NodeTypeId == commentNodeTypeId).ToList();
                var likesForPostAndComments = allCommentsAndLikes.Where(l => l.NodeTypeId == likeTypeId).ToList();
                var likesForPost = likesForPostAndComments.Where(l => RepositoryPath.GetParentPath(RepositoryPath.GetParentPath(l.Path)) == this.ContextNode.Path).ToList();

                var commentMarkupStr = WallHelper.GetCommentMarkupStr();

                // get comments for this content
                contentCommentInfo = new CommentInfo(commentsForPost, likesForPostAndComments, commentMarkupStr);

                // get likes for this content
                contentLikeInfo = new LikeInfo(likesForPost, this.ContextNode.Id);
            }

            using (new OperationTrace("Wall - Content comments markup"))
            {
                var markupStr = WallHelper.GetContentWallMarkup(
                    this.ContextNode,
                    contentCommentInfo.HiddenCommentsMarkup,
                    contentCommentInfo.CommentsMarkup,
                    contentCommentInfo.CommentCount,
                    contentLikeInfo,
                    postsMarkup);

                this.Controls.Add(new Literal { Text = markupStr });
            }

            if (ShowExecutionTime)
                Timer.Stop();

            base.CreateChildControls();
            this.ChildControlsCreated = true;
        }
    }
}
