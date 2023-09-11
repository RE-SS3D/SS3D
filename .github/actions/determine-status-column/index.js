const core = require('@actions/core');
const url = core.getInput('PULL_REQUEST_URL')
const title = core.getInput('ISSUE_TITLE')

var column = null
console.log(url);
console.log(title);

if (url == "") {          // If Pull Request URL is empty, the event must be an issue.
    column = "Backlog";
}
else                        // Otherwise, it's a pull request.
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