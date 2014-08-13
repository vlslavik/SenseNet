using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.Search;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Storage;

namespace SenseNet.Portal.Wall
{
    public class LikeInfo
    {
        private const string LIKELISTLINKPARAMS = "href='javascript:' onclick='SN.Wall.showLikeList($(this), {0});'";

        // =============================================================================== Properties
        public bool iLike { get; private set; }
        public IEnumerable<Node> LikeUsers { get; private set; }
        public int Count { get; private set; }
        

        // =============================================================================== Members
        private string _likeListLinkParams;

        
        // =============================================================================== Public methods
        /// <summary>
        /// Returns markup in a short format - for comments
        /// </summary>
        /// <returns></returns>
        public string GetShortMarkup()
        {
            if (Count == 0)
                return string.Empty;

            string markup = string.Empty;
            if (Count == 1)
                markup = SNSR.GetString(SNSR.Wall.OnePerson);

            if (Count > 1)
                markup = string.Format(SNSR.GetString(SNSR.Wall.NPeople), Count);

            return string.Format("<a {1}>{0}</a>", markup, _likeListLinkParams);
        }
        /// <summary>
        /// Returns markup in a long format - for posts
        /// </summary>
        /// <returns></returns>
        public string GetLongMarkup()
        {
            if (Count == 0)
                return string.Empty;

            string markup = string.Empty;
            if (iLike && Count == 1)
                markup = SNSR.GetString(SNSR.Wall.YouLikeThis);

            if (iLike && Count == 2)
                markup = string.Format(WallHelper.GetXmlDecodedString(SNSR.GetString(SNSR.Wall.YouAndAnotherLikesThis)), _likeListLinkParams);

            if (iLike && Count > 2)
                markup = string.Format(WallHelper.GetXmlDecodedString(SNSR.GetString(SNSR.Wall.YouAndOthersLikesThis)), Count - 1, _likeListLinkParams);

            if (!iLike && Count == 1)
                markup = string.Format(WallHelper.GetXmlDecodedString(SNSR.GetString(SNSR.Wall.OnePersonLikesThis)), _likeListLinkParams);

            if (!iLike && Count > 1)
                markup = string.Format(WallHelper.GetXmlDecodedString(SNSR.GetString(SNSR.Wall.MorePersonLikeThis)), Count, _likeListLinkParams);

            return markup;
        }

        
        // =============================================================================== Constructors
        public LikeInfo()
        {
        }
        public LikeInfo(int parentId)
        {
            _likeListLinkParams = string.Format(LIKELISTLINKPARAMS, parentId);

            var result = DataLayer.GetLikes(parentId);
            if (result == null)
                return;

            iLike = result.Nodes.Any(n => n.CreatedById == User.Current.Id);
            LikeUsers = result.Nodes.Select(n => n.CreatedBy);
            Count = result.Count;
        }
        public LikeInfo(List<Node> likesNodes, int parentId)
        {
            _likeListLinkParams = string.Format(LIKELISTLINKPARAMS, parentId);

            iLike = likesNodes.Any(n => n.CreatedById == User.Current.Id);
            LikeUsers = likesNodes.Select(n => n.CreatedBy);
            Count = likesNodes.Count;
        }
    }
}
