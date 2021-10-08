const fs = require('fs');

const v = process.argv[2];

let prop = fs.readFileSync('./Properties/AssemblyInfo.cs').toString();

prop = prop
	.replace(/AssemblyFileVersion\("([^"\*]+)"\)]/g, `AssemblyFileVersion("${v}")`)
	.replace(/AssemblyVersion\("([^"\*]+)"\)]/g, `AssemblyVersion("${v}")`);

fs.writeFileSync('./Properties/AssemblyInfo.cs', prop);

let manifest = fs.readFileSync('./NextUIPlugin/NextUIPlugin.json').toString();
const dec = JSON.parse(manifest);
dec.AssemblyVersion = v;

fs.writeFileSync('./NextUIPlugin/NextUIPlugin.json', JSON.stringify(dec, null, 4));

const execSync = require('child_process').execSync;

console.log(execSync(`git add Properties/AssemblyInfo.cs`, { encoding: 'utf-8' }));
console.log(execSync(`git add NextUIPlugin/NextUIPlugin.json`, { encoding: 'utf-8' }));
console.log(execSync(`git commit -m v${v}`, { encoding: 'utf-8' }));
console.log(execSync(`git tag -a v${v} -m v${v}`, { encoding: 'utf-8' }));