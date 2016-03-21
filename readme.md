BackendMetadataGenerator
------------------------
Generate some information about properties from C# class.
This information will help to fix response from SOAP server (cast to numbers, booleans, etc.) by xml2js.

USAGE
-----
1. Place C# proxy class files (with *.cs extension) near executable file (BackendMetadataGenerator.exe)
2. Run the program
3. Get `<MethodName>.json` file

The json file contains xpath of response key and value is an object, which contains:  
* `xpath` (string) - part of response path (maybe useful when using validator option)  
* `isArray` (boolean) - indicates that item is array  
* `parse` (string) - javascript type (hint for casting), can be ("number", "boolean", etc.)  

EXAMPLE
-------
```js
var metadata = require("./method.json");
xml2js.parseString(data.response, {
    explicitArray: false,
    ignoreAttrs: false,
    mergeAttrs: true,
    emptyTag: null,
    tagNameProcessors: [xml2js.processors.stripPrefix],
    attrNameProcessors: [xml2js.processors.stripPrefix],
    validator: function(xpath, currentValue, node) {
    	var info = metadata[xpath];
    	// parse node somehow using info
        return node;
    }
}, function(err, result) {});
```
`parseNode` function can be look like:  
```js
function parseNode(info, xpath, currentValue, node) {
    node = replaceNil(node);
    if (info && node !== null) {
        var parse = info.parse;
        switch (parse) {
            case "number": return +node;
            case "boolean": return (node === "true" ? true : (node === "false" ? false : node));
            case "date": 
            default: break;
        }
        if (info.isArray && !Array.isArray(node)) {
            node = nodeToArray(node, xpath);
        }
    }
    return node;
}
```

TODO
----
- Handle XmlElementAttribute properly
- Document methods