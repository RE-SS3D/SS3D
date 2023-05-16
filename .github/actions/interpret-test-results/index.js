function recursivelyOutputFailedTestCases(element) {
    // If this element is a test case, we check it directly, and output if it has failed.
    if (element.tagName == ("TEST-CASE")) {
        if (element.getAttribute("result") == "Failed") {
            output += "....Failed test case: " + element.getAttribute("name") + " (" + element.getAttribute("classname") + ")\n"
            testsFailed = true;
        }
    }
    // If not a test case, it may have subordinate test cases. Check them recursively.
    else
    {
        for (const child of element.children) {
            recursivelyOutputFailedTestCases(child);
        }
    }
}

// Remember: you will probably need to npm install jsdom in the workflow!
const core = require('@actions/core');
const jsdom = require("jsdom");
const fs = require('fs');

// Declare & Initialize variables as required.
var testsFailed = false;         // Needs to be global.
var output = "Test Results:\n";  // Needs to be global.
var passed;
var total;
var name;

// Read the results text, and turn it into Document Object Model (DOM) format
const path = core.getInput('XML_PATH');
const data = fs.readFileSync(path);
const dom = new jsdom.JSDOM(data).window.document;
var testSuites = dom.getElementsByTagName("test-suite")

for (let i = 0; i < testSuites.length; i++) {

    // Cycle through all of the test suites, and filter out the assemblies. Provide summary representative info.
    if (testSuites[i].getAttribute("type") == "Assembly") {
        name = testSuites[i].getAttribute("name");
        passed = testSuites[i].getAttribute("passed");
        total = testSuites[i].getAttribute("total");
        output += "..Assembly: " + name + " (" + passed + "/" + total + ")\n"
        recursivelyOutputFailedTestCases(testSuites[i]);
    }
}

// Summarize and display the output
output += "\nOverall result: ";
if (testsFailed) {
    output += "FAIL!";
    core.setOutput('ALL_TESTS_PASSED', false);
}
else {
    output += "PASS!";
    core.setOutput('ALL_TESTS_PASSED', true);
}
console.log(output);
core.setOutput('DISPLAY_STRING', output);