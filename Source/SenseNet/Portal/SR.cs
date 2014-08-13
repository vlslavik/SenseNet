using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SenseNet.ContentRepository.i18n;

namespace SenseNet.Portal
{
    public class SNSR
    {
        internal class Exceptions
        {
            internal class HttpAction
            {
                public static string NodeIsNotAnApplication_3 = "$Error_Portal:HttpAction_NodeIsNotAnApplication_3";
                public static string NotFound_1 =               "$Error_Portal:HttpAction_NotFound_1";
                public static string Forbidden_1 =              "$Error_Portal:HttpAction_Forbidden_1";
            }
            internal class OData
            {
                public static string InvalidId =                 "$Error_Portal:OData_InvalidId";
                public static string InvalidTopOption =          "$Error_Portal:OData_InvalidTopOption";
                public static string InvalidSkipOption =         "$Error_Portal:OData_InvalidSkipOption";
                public static string InvalidInlineCountOption =  "$Error_Portal:OData_InvalidInlineCountOption";
                public static string InvalidFormatOption =       "$Error_Portal:OData_InvalidFormatOption";
                public static string InvalidOrderByOption =      "$Error_Portal:OData_InvalidOrderByOption";
                public static string ResourceNotFound =          "$Error_Portal:OData_ResourceNotFound";
                public static string ResourceNotFound_2 =        "$Error_Portal:OData_ResourceNotFound_2";
                public static string CannotConvertToJSON_2 =     "$Error_Portal:OData_CannotConvertToJSON_2";
                public static string ContentAlreadyExists_1 =    "$Error_Portal:OData_ContentAlreadyExists_1";

                public static string RestoreExistingName =         "$Error_Portal:OData_Restore_ExistingName";
                public static string RestoreForbiddenContentType = "$Error_Portal:OData_Restore_ForbiddenContentType";
                public static string RestoreNoParent =             "$Error_Portal:OData_Restore_NoParent";
                public static string RestorePermissionError =      "$Error_Portal:OData_Restore_PermissionError";
            }
            internal class Site
            {
                public static string UrlListCannotBeEmpty =        "$Error_Portal:Site_UrlListCannotBeEmpty";
                public static string StartPageMustBeUnderTheSite = "$Error_Portal:Site_StartPageMustBeUnderTheSite";
                public static string UrlAlreadyUsed_2 =            "$Error_Portal:Site_UrlAlreadyUsed_2";
            }
            internal class ContentView
            {
                public static string InvalidDataHead = "$Error_Portal:ContentView_InvalidDataHead";
            }
            internal class Operations
            {
                internal static string ContentDoesNotExistWithPath_1 = "$Error_Portal:ContentDoesNotExistWithPath_1";
            }
        }
        internal class FieldControls
        {
            public static string Number_ValidFormatIs = "$Field:Number_ValidFormatIs";

            public static string HyperLink_TextLabel = "$Field:HyperLink_TextLabel";
            public static string HyperLink_HrefLabel = "$Field:HyperLink_HrefLabel";
            public static string HyperLink_HrefImageLabel = "$Field:HyperLink_HrefImageLabel";
            public static string HyperLink_TargetLabel = "$Field:HyperLink_TargetLabel";
            public static string HyperLink_TitleLabel = "$Field:HyperLink_TitleLabel";

            public static string TagList_AddTag = "$Field:TagList_AddTag";
            public static string TagList_BlacklistedTag = "$Field:TagList_BlacklistedTag";
            public static string TagList_BlacklistedTags = "$Field:TagList_BlacklistedTags";
        }
        internal class Wall
        {
            public static string OnePerson = "$Wall:OnePerson";
            public static string NPeople = "$Wall:NPeople";
            public static string YouLikeThis = "$Wall:YouLikeThis";
            public static string YouAndAnotherLikesThis = "$Wall:YouAndAnotherLikesThis";
            public static string YouAndOthersLikesThis = "$Wall:YouAndOthersLikesThis";
            public static string OnePersonLikesThis = "$Wall:OnePersonLikesThis";
            public static string MorePersonLikeThis = "$Wall:MorePersonLikeThis";
            public static string CommentedOnAContent = "$Wall:CommentedOnAContent";
            public static string LikesAContent = "$Wall:LikesAContent";
            public static string PleaseLogIn = "$Wall:PleaseLogIn";

            public static string What_Created = "$Wall:What_Created";
            public static string What_Modified = "$Wall:What_Modified";
            public static string What_Deleted = "$Wall:What_Deleted";
            public static string What_Moved = "$Wall:What_Moved";
            public static string What_Copied = "$Wall:What_Copied";
            public static string What_To = "$Wall:What_To";

            public static string Source = "$Wall:Source";
            public static string Target = "$Wall:Target";
        }

        public class Portlets
        {
            public class ContentCollection
            {
                public static string ErrorLoadingContentView = "$ContentCollectionPortlet:ErrorContentView";
                public static string ErrorInvalidContentQuery = "$ContentCollectionPortlet:InvalidContentQuery";
            }
        }

        public static string GetString(string fullResourceKey)
        {
            return SenseNetResourceManager.Current.GetString(fullResourceKey);
        }
        public static string GetString(string className, string name)
        {
            return SenseNetResourceManager.Current.GetString(className, name);
        }
        public static string GetString(string fullResourceKey, params object[] args)
        {
            return String.Format(SenseNetResourceManager.Current.GetString(fullResourceKey), args);
        }

    }
}
