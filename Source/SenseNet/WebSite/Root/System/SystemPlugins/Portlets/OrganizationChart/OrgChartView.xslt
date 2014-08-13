<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl" xmlns:snc="sn://SenseNet.Portal.UI.ContentTools"
                xmlns:snct="sn://SenseNet.Portal.UI.ContentTools">
  <xsl:output method="xml" indent="yes"/>


  <xsl:template match="/">
    <link type="text/css" rel="stylesheet" href="/Root/Global/styles/sn-org-chart.css" />
    <!--<textarea>
      <xsl:copy-of select="/"/>
    </textarea>-->
    <xsl:choose>
      <xsl:when test="snct:UserIsLoggedIn() = 'true'">
    <div class="sn-orgc">
      <xsl:apply-templates select="Content" />
    </div>
      </xsl:when>
      <xsl:otherwise>
        <xsl:variable name="PleaseLogin" select="snc:GetResourceString('$Renderers, PleaseLogin')" />
        <xsl:value-of select="$PleaseLogin" />
      </xsl:otherwise>
    </xsl:choose>
  </xsl:template>

  <xsl:template match="Content">


        <table cellspacing="0" cellpadding="0" border="0" style="width: 100%; border-collapse: collapse;">
          <tr>
            <td colspan="{count(Employees/Content)}" align="center">
              <div class="sn-orgc-card">
                <a class="sn-orgc-userlink" href="{Actions/Browse}">
                  <div class="sn-pic-left">
                    <xsl:choose>
                      <xsl:when test="Fields/Avatar[@imageMode = 'BinaryData']">
                        <xsl:variable name="UserImage" select="snc:GetResourceString('$ContentView, UserImage')" />
                        <img src="{Fields/Avatar}" width="64" height="64" alt="{$UserImage}" />
                      </xsl:when>
                      <xsl:when test="Fields/Avatar[@imageMode = 'Reference']">
                        <xsl:variable name="UserImage" select="snc:GetResourceString('$ContentView, UserImage')" />
                        <img src="{Fields/ImageRef/Path}" width="64" height="64" alt="{$UserImage}" />
                      </xsl:when>
                      <xsl:otherwise>
                        <xsl:variable name="MissingUserImage" select="snc:GetResourceString('$ContentView, MissingUserImage')" />
                        <img src="/Root/Global/images/orgc-missinguser.png" width="64" height="64" alt="{$MissingUserImage}" />

                      </xsl:otherwise>
                    </xsl:choose>
                  </div>
                  <div class="sn-orgc-name sn-content">
                    <h1 class="sn-content-title">
                      <xsl:value-of select="Fields/FullName"/>
                    </h1>
                    <!--<h2 class="sn-content-subtitle">
                  <xsl:value-of select="Fields/Domain"/>\<xsl:value-of select="ContentName"/>
                </h2>-->
                  </div>
                  <div class="sn-orgc-position">
                    <span>
                      <xsl:if test="Fields/JobTitle != ''">
                        <xsl:value-of select="Fields/JobTitle" disable-output-escaping="no"/>
                      </xsl:if>
                    </span>
                  </div>
                </a>
              </div>
            </td>
          </tr>

          <xsl:if test="Employees/Content">
            <tr>
              <td colspan="{count(Employees/Content)}" align="center" style="margin: 0; padding: 0; height: 30px;">
                <span class="sn-orgc-border-vertical" style="height: 30px; display: block; width: 1px; margin: 0; padding: 0;"></span>
              </td>
            </tr>

            <xsl:if test="count(Employees/Content)>1">
              <tr>
                <xsl:variable name="maxChildren" select="count(Employees/Content)"/>
                <xsl:for-each select="Employees/Content">
                  <xsl:if test="position()=1">
                    <td align="right" style="display: block; margin: 0; padding: 0; height: 0;">
                      <span class="sn-orgc-border-horizontal"  style="height: 0; display: block; width: 50%; margin: 0; padding: 0;"></span>
                    </td>
                  </xsl:if>
                  <xsl:if test="position()>1 and position()&lt;$maxChildren">
                    <td style="height: 0; margin: 0; padding: 0; height: 0;">
                      <span class="sn-orgc-border-horizontal" style="height: 0; display: block; width: 100%; margin: 0; padding: 0;"></span>
                    </td>
                  </xsl:if>
                  <xsl:if test="position()=$maxChildren and position()>1">
                    <td align="left" style="height: 0; margin: 0; padding: 0; height: 0;">
                      <span class="sn-orgc-border-horizontal"  style="height: 0; display: block; width: 50%; margin: 0; padding: 0;"></span>
                    </td>
                  </xsl:if>
                </xsl:for-each>
              </tr>

              <tr>
                <xsl:for-each select="Employees/Content">
                  <td align="center" style="height: 30px; margin: 0; padding: 0;">
                    <span class="sn-orgc-border-vertical" style="height: 30px; display: block; width: 1px; margin: 0; padding: 0;"></span>
                  </td>
                </xsl:for-each>
              </tr>
            </xsl:if>
            <tr>
              <xsl:for-each select="Employees/Content">
                <td valign="top">
                  <xsl:apply-templates select="."></xsl:apply-templates>
                </td>
              </xsl:for-each>
            </tr>
          </xsl:if>
        </table>
      

  </xsl:template>

</xsl:stylesheet>
