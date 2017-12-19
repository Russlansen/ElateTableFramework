using ElateTableFramework.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;
namespace ElateTableFramework
{
    public static class TableHelper
    {
        public static MvcHtmlString ElateTable<T>(this HtmlHelper html, IEnumerable<T> entities) where T : class
        {
            TagBuilder table = new TagBuilder("table");   

            Type entityType = entities.FirstOrDefault().GetType();

            var properties = entityType.GetProperties();
            
            table.InnerHtml += BuildTHeadTag(properties);

            table.InnerHtml += BuildTBodyTag(entities, properties);

            return new MvcHtmlString(table.ToString());
        }

        private static TagBuilder BuildTHeadTag(PropertyInfo[] properties)
        {
            TagBuilder thead = new TagBuilder("thead");
            TagBuilder trHead = new TagBuilder("tr");
            foreach (var property in properties)
            {
                if (IsExcluded(property)) continue;

                TagBuilder td = new TagBuilder("td");
                td.SetInnerText(GetTHeadName(property));
                trHead.InnerHtml += td;
            }
            thead.InnerHtml += trHead;
            return thead;
        }

        private static TagBuilder BuildTBodyTag<T>(IEnumerable<T> entities, PropertyInfo[] properties) where T : class
        {
            TagBuilder tbody = new TagBuilder("tbody");
            foreach (var entity in entities)
            {
                TagBuilder tr = new TagBuilder("tr");
                foreach (var property in properties)
                {
                    if (IsExcluded(property)) continue;

                    var value = property.GetValue(entity);
                    TagBuilder td = new TagBuilder("td");
                    td.SetInnerText(value.ToString());
                    tr.InnerHtml += td;
                }
                tbody.InnerHtml += tr;
            }
            return tbody;
        }

        private static bool IsExcluded(PropertyInfo property)
        {
            var attributes = property.GetCustomAttributes(false);
            foreach (var attribute in attributes)
            {
                if (attribute is ElateExcludePropertyAttribute)
                    return true;
            }
            return false;
        }

        private static string GetTHeadName(PropertyInfo property)
        {
            string name = null;
            string mergingName = null;
            var attributes = property.GetCustomAttributes(false);
            foreach(var attribute in attributes)
            {
                if(attribute is ElatePropertyRenameAttribute)
                {
                    var attr = attribute as ElatePropertyRenameAttribute;
                    name = attr.Name;
                }
                if(attribute is ElateMergeAttribute)
                {
                    var attr = attribute as ElateMergeAttribute;
                    mergingName = attr.MergingColumnName;
                }
            }
            return mergingName ?? name ?? property.Name;
        }
    }
}