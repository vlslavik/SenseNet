<%@ Control Language="C#" AutoEventWireup="true" Inherits="System.Web.UI.UserControl" %>
<script>
    $(window).load(function ()
    {
        $.getScript("http://www.sensenet.com/Root/Sites/Default_Site/version.js?callback=?", function ()
        {

            var name = (SenseNetVersion.name);
            var versionnumber = (SenseNetVersion.versionnumber);
            var version = (SenseNetVersion.versionnumber).replace(/\D/g, '');
            var edition = (SenseNetVersion.edition);
            var releasedate = (SenseNetVersion.releasedate);
            var changeloglink = (SenseNetVersion.changeloglink);
            var downloadlink = (SenseNetVersion.downloadlink);
            var status = (SenseNetVersion.status);

            var currentVersion = $(".sn-copyright a:last").text();
            var currentVersionnum = currentVersion.replace(/\D/g, '');

            if (currentVersionnum.toString().length != 2)
            {
                //alert(currentVersionnum.length);
                currvernum = currentVersionnum;
            }
            else
            {
                currvernum = currentVersionnum + "0";
            }
            if (version.toString().length != 2)
            {

                version = version;
            }
            else
            {
                version = version + "0";
            }

            if (currvernum == null && status == "stable")
            {
                $('.info').hide();
            }
            else if (version < currvernum && status == "stable")
            {
                $('.info').hide();
            }
            else if (currvernum == version && status != "stable")
            {
                $('.versionInfo .info').addClass('unstable');
                $('.info').html('This is the latest version of Sense/Net ECM Community Edition').css('display', 'block');
                $('.arrow').css('display', 'block');
            }
            else if (version > currvernum && status == "stable")
            {
                $('.info').html('<h1>Available new versions</h1><ul><li><b>' + name + ' ' + versionnumber + ' ' + edition + '</b></li><li>' + releasedate + ' - ' + status + '</li><li class="last"><a href="">changelog</a>  |  <a href="">download</a></li></ul>').css('display', 'block');
                $('.info .last a:first').attr('href', changeloglink);
                $('.info .last a:last').attr('href', downloadlink);
            }
            else if (version > currvernum && status != "stable")
            {
                $('.info').html('<h1>Available new versions</h1><ul><li><b>' + name + ' ' + versionnumber + ' ' + edition + '</b></li><li>' + releasedate + ' - ' + status + '</li><li class="last"><a href="">changelog</a>  |  <a href="">download</a></li></ul>').css('display', 'block');
                $('.info .last a:first').attr('href', changeloglink);
                $('.info .last a:last').attr('href', downloadlink);
                $('.versionInfo .info').addClass('unstable');
            }
            else
            {
                $('.versionInfo .info').addClass('latest');
                $('.info').html('This is the latest version of Sense/Net ECM Community Edition').css('display', 'block');
                $('.arrow').css('display', 'block');
                if (status != "stable")
                {
                    $('.versionInfo .info').addClass('unstable');
                    $('.info .last a:first').attr('href', changeloglink);
                }
            }

        });


    });
    
    

    
    

</script>
<style>
.versiondiv{position: absolute;top: 5px;left: 33%;}
.versionInfo {color: #fff;z-index:1000000}
/*.ie9 .versionInfo{width: 254px;}*/
.info {margin-left: 30px;;position: relative;background: #f15a24;padding: 10px;border: solid 1px #f15a24;-webkit-border-radius: 5px;-moz-border-radius: 5px;border-radius: 5px;min-height: 20px;}
.info:after {right: 100%;border: solid transparent;content: " ";height: 0;width: 0;position: absolute;pointer-events: none;}
.info:after {border-color: border-color: rgba(136, 183, 213, 0);;border-right-color: #f15a24;border-width: 16px;top: 50%;margin-top: -16px;}
.versionInfo .info h1 {color: #fff;font-size: 14px;margin: 0px 0px 5px 0px;border-bottom: solid 1px #fff;padding-bottom: 3px;}
.versionInfo .info ul {margin: 0;padding: 0;}
.versionInfo .info ul li {color: #fff;list-style-type: none;margin-bottom: 5px;padding: 0;}
.versionInfo .info ul li.links {margin-bottom: 0;}
.versionInfo a {color: #fff;text-decoration: underline;font-weight: bold;}
.info.latest:after {border-right-color: #64A1CB;}
.info.latest {background:#64A1CB;border-color: #64A1CB;float: right;width: 196px;}
.info.unstable {background: #ff0000 !important;border: solid 1px #ff0000 !important}
.info.unstable:after {border-right-color: #ff0000;}
</style>

<% string user = (SenseNet.ContentRepository.User.Current).ToString(); %>
<%if (user == "Admin")
  {%>

    <div class="sn-hide"></div>
    <div class="info" style="display: none"></div>

    
<%} %>