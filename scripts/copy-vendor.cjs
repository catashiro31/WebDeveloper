const fs = require('node:fs');
const path = require('node:path');
const source = path.dirname(require.resolve('toastify-js/package.json'));
const target = path.join(__dirname, '../WebDeveloper/wwwroot/lib/toastify');
fs.mkdirSync(target, {recursive: true});
for (const [from, to] of [['src/toastify.js', 'toastify.js'], ['src/toastify.css', 'toastify.css'], ['LICENSE', 'LICENSE']]) {
    fs.copyFileSync(path.join(source, from), path.join(target, to));
}
