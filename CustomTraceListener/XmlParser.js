       function format(value, style)
        {
            return "<span class=\"" + style + "\">" + value + "</span>";
        }

        function formatNodeName(value)
        {
            return format(value, "nodeName");
        }

        function formatNodeValue(value)
        {
            return format(value, "nodeValue");
        }

        function formatAttrName(value)
        {
            return format(value, "attrName");
        }

        function formatAttrValue(value)
        {
            return "&quot;" + format(value, "attrValue") + "&quot;";
        }

        function getAttributeHtml(attributes)
        {
            if (attributes == null)
                return "";

            var html = "";
            $(attributes).each(function(i,e)
            {
                var attrName = $(this)[0].nodeName;
                var attrValue = $(this)[0].nodeValue;
                //if (!attrName.match(/xmlns|:/)) {
                //    html += "&nbsp;" + formatAttrName(attrName) + "=" + formatAttrValue(attrValue);
                //}
                html += "&nbsp;" + formatAttrName(attrName) + "=" + formatAttrValue(attrValue);
            });
            return html;
        }

        function getSpaces(level)
        {
            var spaces = "";
            for (i = 0; i < level; i++)
            {
                spaces += " ";
            }
            return spaces;
        }
        function IndentXml(node, container, level)
        {
            container = container || document.createElement('div');
            level = level || 0;
            var thisNode = $(node);
            var attrs = thisNode[0].attributes;
            $(container).append("&lt;" + formatNodeName(thisNode[0].nodeName) + getAttributeHtml(attrs) + "&gt;");
            var children = thisNode.children();
            if (children.length > 0) {
                children.each(function (i, e) {
                    $(container).append("<br/>");
                    var childContainer = $(container).append(getSpaces(level * 2) + "<span></span>");
                    //childContainer.css("margin-left", level * 5 + "px");
                    IndentXml($(this), childContainer, level+1);
                });
                $(container).append("<br/>" + getSpaces((level-1) * 2));
                
            }
            else
            {
                //container.append("<p>" + thisNode.text() + "</p>");
                container.append(formatNodeValue(thisNode.text()));
            }
            $(container).append("&lt;/" + formatNodeName(thisNode[0].nodeName) + "&gt;");
            return container;
        }