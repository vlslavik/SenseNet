using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.i18n;
using SenseNet.ContentRepository.Storage;
using SenseNet.Portal.Workspaces;

namespace SenseNet.Portal.Wall
{
    public enum PostType
    {
        BigPost = 0,
        JournalCreated,
        JournalModified,
        JournalDeletedPhysically,
        JournalMoved,
        JournalCopied
    }

    public class PostInfo
    {
        // =============================================================================== Properties
        public int Id { get; private set; }
        public string Path { get; private set; }
        public string ClientId { get; private set; }
        public int JournalId { get; private set; }
        public DateTime CreationDate { get; private set; }
        public User CreatedBy { get; private set; }
        public string Text { get; private set; }
        public PostType Type { get; private set; }
        public Node SharedContent { get; private set; }
        public string Action { get; private set; }
        public string Details { get; private set; }
        public string LastPath { get; private set; }
        public bool IsJournal { get; private set; }


        // =============================================================================== Static methods
        public static int GetIdFromClientId(string clientId)
        {
            var idStr = clientId;
            if (clientId.StartsWith("J"))
                idStr = clientId.Substring(1);
            return Convert.ToInt32(idStr);
        }
        public static bool IsJournalId(string clientId)
        {
            return clientId.StartsWith("J");
        }


        // =============================================================================== Constructors
        public static PostInfo CreateFromCommentOrLike(Node commentOrLike)
        {
            var targetContent = commentOrLike.Parent.Parent;

            // comment likes should not appear, only content likes
            if (targetContent.NodeType.Name == "Comment")
                return null;

            var postInfo = new PostInfo();
            postInfo.CreationDate = commentOrLike.CreationDate;
            postInfo.CreatedBy = commentOrLike.CreatedBy as User;
            //postInfo.Action = commentOrLike.NodeType.Name == "Comment" ? "commented on a Content" : "likes a Content";
            postInfo.Action = SNSR.GetString(commentOrLike.NodeType.Name == "Comment" ? SNSR.Wall.CommentedOnAContent : SNSR.Wall.LikesAContent);
            postInfo.Id = targetContent.Id;
            postInfo.Path = targetContent.Path; 
            postInfo.ClientId = postInfo.Id.ToString();
            postInfo.Type = PostType.BigPost;
            postInfo.SharedContent = targetContent;
            return postInfo;
        }
        private PostInfo()
        { 
        }
        public PostInfo(Node node)
        {
            CreationDate = node.CreationDate;
            CreatedBy = node.CreatedBy as User;
            Text = node.GetProperty<string>("Description");
            Details = node.GetProperty<string>("PostDetails"); 
            JournalId = node.GetProperty<int>("JournalId"); // journal's id to leave out item from wall if post already exists
            Id = node.Id;
            Path = node.Path; 
            ClientId = Id.ToString();
            Type = (PostType)node.GetProperty<int>("PostType");
            SharedContent = node.GetReference<Node>("SharedContent");
            if (SharedContent != null) {
                LastPath = SharedContent.Path;
                var ws = SenseNet.ContentRepository.Workspaces.Workspace.GetWorkspaceWithWallForNode(node);
                if (ws != null)
                    Action = string.Format("{2} <a href='{0}'>{1}</a>", ws.Path, Content.Create(ws).DisplayName, SNSR.GetString(SNSR.Wall.What_To));
            }
        }
        public PostInfo(JournalItem journalItem, string lastPath)
        {
            IsJournal = true;
            LastPath = lastPath;
            CreationDate = journalItem.When;
            var backspIndex = journalItem.Who.IndexOf('\\');
            if (backspIndex != -1)
            {
                var domain = journalItem.Who.Substring(0, backspIndex);
                var name = journalItem.Who.Substring(backspIndex + 1);
                CreatedBy = User.Load(domain, name);
            }

            //var contentName = string.Empty;
            //if (journalItem.Wherewith.StartsWith(contextPath))
            //{
            //    contentName = journalItem.Wherewith.Substring(contextPath.Length).TrimStart('/');
            //    // if workspace relative path is empty, the context path is the workspace itself
            //    if (string.IsNullOrEmpty(contentName))
            //        contentName = RepositoryPath.GetFileName(contextPath);
            //}
            //else
            //{
            //    contentName = RepositoryPath.GetFileName(journalItem.Wherewith);
            //}

            var what = journalItem.What.ToLower();
            var typeStr = string.Empty;

            // type & type string
            if (what == "created")
            {
                Type = PostType.JournalCreated;
                typeStr = SNSR.GetString(SNSR.Wall.What_Created);
            }
            else if (what == "modified")
            {
                Type = PostType.JournalModified;
                typeStr = SNSR.GetString(SNSR.Wall.What_Modified);
            }
            else if (what == "deleted" || what == "deletedphysically")
            {
                Type = PostType.JournalDeletedPhysically;
                typeStr = SNSR.GetString(SNSR.Wall.What_Deleted);
            }
            else if (what == "moved")
            {
                Type = PostType.JournalMoved;
                typeStr = SNSR.GetString(SNSR.Wall.What_Moved);
            }
            else if (what == "copied")
            {
                Type = PostType.JournalCopied;
                typeStr = SNSR.GetString(SNSR.Wall.What_Copied);
            }
            else
            {
                throw new NotImplementedException(String.Format("Processing '{0}' journal type is not implemented", what));
            }

            var displyaName = SenseNetResourceManager.Current.GetString(journalItem.DisplayName);
            var targetDisplayName = SenseNetResourceManager.Current.GetString(journalItem.TargetDisplayName);

            Text = Type == PostType.JournalCopied || Type == PostType.JournalMoved ?
                string.Format("{0} <a href='{1}'>{2}</a> {5} <a href='{3}'>{4}</a>", typeStr, "{{path}}", displyaName, journalItem.TargetPath, targetDisplayName, SNSR.GetString(SNSR.Wall.What_To)) :
                string.Format("{0} <a href='{1}'>{2}</a>", typeStr, "{{path}}", displyaName);

            JournalId = journalItem.Id; // journal's id to leave out item from wall if post already exists
            Id = journalItem.Id;
            ClientId = "J" + Id.ToString();
            Path = lastPath;

            // details
            switch (Type)
            {
                case PostType.JournalModified:
                    Details = journalItem.Details;
                    break;
                case PostType.JournalMoved:
                    Details = string.Format("{2}: <a href='{0}'>{0}</a><br/>{3}: <a href='{1}'>{1}</a>", RepositoryPath.GetParentPath(journalItem.SourcePath), journalItem.TargetPath, SNSR.GetString(SNSR.Wall.Source), SNSR.GetString(SNSR.Wall.Target));
                    break;
                case PostType.JournalCopied:
                    Details = string.Format("{2}: <a href='{0}'>{0}</a><br/>{3}: <a href='{1}'>{1}</a>", RepositoryPath.GetParentPath(journalItem.Wherewith), journalItem.TargetPath, SNSR.GetString(SNSR.Wall.Source), SNSR.GetString(SNSR.Wall.Target));
                    break;
                default:
                    break;
            }

            //SharedContent = Node.LoadNode(journalItem.Wherewith);
        }
    }
}
