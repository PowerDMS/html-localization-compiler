var parseLocalizationTags = (function () {
    'use strict';

    /**
     * parseLocalizationTags removes brackets and translator notes from html so that
     * compilation of translated html files is not required during the development
     * process
     *
     * translation tags are of the format: [[ english version | translator notes ]]
     * this filter will cause these tags to be rendered as: english version
     */
    var parseLocalizationTags = function(data) {

        if (data && typeof data === 'string') {
            data = data.replace(/(?:'"|"')(?:\[\[)([^\|]*)(?:\|[^\]]*]])(?:'"|"')/g, handleDoubleWrappedTags);
            data = data.replace(/(?:\[\[)([^\|]*)(?:\|[^\]]*]])/g, handleRegularTags);
        }

        return data;
    };

    var handleDoubleWrappedTags = function(wholeTag, contents) {
        return '"\'' + contents.replace(/(?:\[\[)([^\]]*)(?:]])/g, '\' + $1 + \'') + '\'"';
    };

    var handleRegularTags = function(wholeTag, contents) {
        return contents.replace(/(?:\[\[)([^\]]*)(?:]])/g, '{{$1}}');
    };

    $(document).ready(function () {
        document.body.innerHTML = parseLocalizationTags(document.body.innerHTML);
    });

    return parseLocalizationTags;
})();