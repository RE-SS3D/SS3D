const core = require('@actions/core');
const results = JSON.parse(core.getInput('JSON_FIELD_OPTIONS'))
const target = core.getInput('TARGET_OPTION').toUpperCase()

var fieldId = null
var lengthOfTarget = target.length;
console.log('Target = ' + target)
for (let i = 0; i < results.node.options.length; i++) {
    console.log(results.node.options[i].name.toUpperCase().slice(-lengthOfTarget))
    if (target == results.node.options[i].name.toUpperCase().slice(-lengthOfTarget)) {
        fieldId = results.node.options[i].id
    }
}
core.setOutput('OPTION_ID', fieldId);