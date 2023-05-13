const core = require('@actions/core');
const results = JSON.parse(core.getInput('JSON_LIST_OF_PROJECTS'))
const target = core.getInput('PROJECT_NAME').toUpperCase()

var existingProjectId = null
for (let i = 0; i < results.organization.projectsV2.nodes.length; i++) {
    console.log( results.organization.projectsV2.nodes[i].title.toUpperCase() )
    if (target == results.organization.projectsV2.nodes[i].title.toUpperCase()) {
        existingProjectId = results.organization.projectsV2.nodes[i].id
    }
}
core.setOutput('EXISTING_PROJECT_ID', existingProjectId);