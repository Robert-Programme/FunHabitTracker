const { test } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const script = fs.readFileSync(path.resolve(__dirname, '../../src/FunHabitTracker/wwwroot/js/site.js'), 'utf8');

test('shared script loads on pages without legacy habit form elements', () => {
    const ready = [];
    const document = {
        getElementById: () => null,
        addEventListener: (event, callback) => {
            if (event === 'DOMContentLoaded') ready.push(callback);
        }
    };
    assert.doesNotThrow(() => {
        vm.runInNewContext(script, { document });
        for (const callback of ready) callback();
    });
});
