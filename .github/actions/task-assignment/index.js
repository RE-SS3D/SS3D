const core = require('@actions/core');
const github = require('@actions/github');
const { Octokit } = require('@octokit/core');

import fetch from "node-fetch";

async function addAssignee(octokit, Owner, Repo, IssueNumber, Assignee) {
    await octokit.request('POST /repos/{owner}/{repo}/issues/{issue_number}/assignees', {
        owner: Owner,
        repo: Repo,
        issue_number: IssueNumber,
        fetch: fetch,
        assignees: [
            Assignee
        ],
        headers: {
            'X-GitHub-Api-Version': '2022-11-28'
        }
    })
}

async function removeAssignee(octokit, Owner, Repo, IssueNumber, Assignee) {
    await octokit.request('DELETE /repos/{owner}/{repo}/issues/{issue_number}/assignees', {
        owner: Owner,
        repo: Repo,
        issue_number: IssueNumber,
        assignees: [
            Assignee
        ],
        headers: {
            'X-GitHub-Api-Version': '2022-11-28'
        }
    })
}

async function addIssueComment(octokit, Owner, Repo, IssueNumber, Body) {
    await octokit.request('POST /repos/{owner}/{repo}/issues/{issue_number}/comments', {
        owner: Owner,
        repo: Repo,
        issue_number: IssueNumber,
        body: Body,
        headers: {
            'X-GitHub-Api-Version': '2022-11-28'
        }
    })
}

try {
    // Octokit.js
    // https://github.com/octokit/core.js#readme
    const token = core.getInput('token');
    const octokit = new Octokit({
        auth: token
    })

    // Get all of the info we need from the context.
    comment = github.context.payload.comment.body.toUpperCase();
    owner = github.context.payload.repository.owner.login;
    repo = github.context.payload.repository.name;
    issue = github.context.payload.issue.number;
    author = github.context.payload.comment.user.login;
    assignees = github.context.payload.issue.assignees;

    // Confirm whether author is currently assigned
    assigned = false;
    for (let i = 0; i < assignees.length; i++) {
        if (assignees[i].login == author) {
            assigned = true;
        }
    }

    // Determine what we need to do based on the comment. Note that 'UNASSIGN ME' must be first,
    // because 'ASSIGN ME' will always be a substring of that.
    if (comment.includes("UNASSIGN ME")) {
        if (assigned == true) {
            removeAssignee(octokit, owner, repo, issue, author);
        } else {
            addIssueComment(octokit, owner, repo, issue, author + ": you cannot be unassigned as you are not currently assigned.");
        }
    } else if (comment.includes("ASSIGN ME")) {
        if (assignees.length == 0) {
            addAssignee(octokit, owner, repo, issue, author);
        } else if (assigned == true) {
            addIssueComment(octokit, owner, repo, issue, author + ": you are already assigned.");
        } else {
            addIssueComment(octokit, owner, repo, issue, "Sorry " + author + ", other contributors are already assigned to this issue.");
        }
    }
} catch (error) {
    core.setFailed(error.message);
}

