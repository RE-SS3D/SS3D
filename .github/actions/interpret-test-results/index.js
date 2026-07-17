const core = require('@actions/core');
const { JSDOM } = require('jsdom');
const fs = require('fs');

const path = core.getInput('XML_PATH');
let output = 'Test Results:\n';
let testsFailed = false;
const failedTests = [];
const inconclusiveTests = [];

function isTestCase(element) {
    return element.tagName && element.tagName.toUpperCase() === 'TEST-CASE';
}

function formatAssemblySummary(suite) {
    const name = suite.getAttribute('name');
    const passed = suite.getAttribute('passed') ?? '0';
    const failed = suite.getAttribute('failed') ?? '0';
    const inconclusive = suite.getAttribute('inconclusive') ?? '0';
    const skipped = suite.getAttribute('skipped') ?? '0';
    const total = suite.getAttribute('total') ?? '0';

    return `..Assembly: ${name} (${passed} passed, ${failed} failed, ${inconclusive} inconclusive, ${skipped} skipped / ${total} total)`;
}

function collectTestCaseResults(element) {
    if (isTestCase(element)) {
        const result = element.getAttribute('result');
        const name = element.getAttribute('name');
        const classname = element.getAttribute('classname');

        if (result === 'Failed') {
            testsFailed = true;
            failedTests.push({ name, classname });
            output += `....Failed test case: ${name} (${classname})\n`;
        } else if (result === 'Inconclusive') {
            inconclusiveTests.push({ name, classname });
        }
        return;
    }

    for (const child of element.children) {
        collectTestCaseResults(child);
    }
}

function writeStepSummary(markdown) {
    const summaryPath = process.env.GITHUB_STEP_SUMMARY;
    if (!summaryPath) {
        return;
    }

    fs.appendFileSync(summaryPath, markdown);
}

if (!fs.existsSync(path)) {
    const message = `Test results file not found at ${path}. Unity tests may not have run — check UNITY_LICENSE (or UNITY_SERIAL) and related secrets in the unity_tests environment.`;
    core.warning(message);
    console.log(message);
    core.setOutput('DISPLAY_STRING', message);
    core.setOutput('ALL_TESTS_PASSED', false);
    writeStepSummary(`## Test Results\n\n${message}\n`);
    process.exit(0);
}

const data = fs.readFileSync(path);
const dom = new JSDOM(data, { contentType: 'application/xml' }).window.document;
const testSuites = dom.getElementsByTagName('test-suite');

for (let i = 0; i < testSuites.length; i++) {
    if (testSuites[i].getAttribute('type') === 'Assembly') {
        output += `${formatAssemblySummary(testSuites[i])}\n`;
        collectTestCaseResults(testSuites[i]);
    }
}

const testRun = dom.getElementsByTagName('test-run')[0];
if (testRun) {
    const passed = testRun.getAttribute('passed') ?? '?';
    const failed = testRun.getAttribute('failed') ?? '?';
    const inconclusive = testRun.getAttribute('inconclusive') ?? '?';
    const skipped = testRun.getAttribute('skipped') ?? '?';
    const total = testRun.getAttribute('total') ?? '?';
    output += `\nTotals: ${passed} passed, ${failed} failed, ${inconclusive} inconclusive, ${skipped} skipped / ${total} total\n`;
}

output += '\nOverall result: ';
if (testsFailed) {
    output += 'FAIL!';
    core.setOutput('ALL_TESTS_PASSED', false);
} else {
    output += 'PASS!';
    core.setOutput('ALL_TESTS_PASSED', true);
}

console.log(output);
core.setOutput('DISPLAY_STRING', output);

let summary = '## Test Results\n\n';
if (testRun) {
    summary += `| Passed | Failed | Inconclusive | Skipped | Total |\n`;
    summary += `| --- | --- | --- | --- | --- |\n`;
    summary += `| ${testRun.getAttribute('passed') ?? '?'} | ${testRun.getAttribute('failed') ?? '?'} | ${testRun.getAttribute('inconclusive') ?? '?'} | ${testRun.getAttribute('skipped') ?? '?'} | ${testRun.getAttribute('total') ?? '?'} |\n\n`;
}

summary += `**Overall:** ${testsFailed ? 'FAIL' : 'PASS'}  \n`;

if (failedTests.length > 0) {
    summary += '\n### Failed tests\n\n';
    for (const test of failedTests) {
        summary += `- \`${test.name}\` (${test.classname})\n`;
    }
}

if (inconclusiveTests.length > 0) {
    summary += '\n### Inconclusive tests\n\n';
    summary += '_Inconclusive results do not fail the run. On CI, the compiled-player prerequisite is expected when no build artifact is present._\n\n';
    for (const test of inconclusiveTests) {
        summary += `- \`${test.name}\` (${test.classname})\n`;
    }
}

summary += '\n<details><summary>Assembly breakdown</summary>\n\n```\n';
summary += output;
summary += '\n```\n</details>\n';

writeStepSummary(summary);
