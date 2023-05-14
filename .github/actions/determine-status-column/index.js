const core = require('@actions/core');
const event = core.getInput('GITHUB_EVENT_NAME')
const title = core.getInput('ISSUE_TITLE')

var column = null
console.log(event);
console.log(title);

if (event == "issue") {
    column = "Backlog";
}
else if (event == "pull_request")
{
    var potentialPrefix = title.toUpperCase().slice(0, 5);
    console.log(potentialPrefix);
    if (potentialPrefix == "[WIP]") {
        column = "In Progress";
    }
    else
    {
        column = "In Review";
    }
}
console.log(column);
core.setOutput('COLUMN', column);