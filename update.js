const fs = require('fs');

const v = process.argv[2];

const projsToUpdate = [
	'./NextUIPlugin/NextUIPlugin.csproj',
	'./NextUIBrowser/NextUIBrowser.csproj',
	'./NextUIShared/NextUIShared.csproj',
]

for (const csproj of projsToUpdate) {
	let prop = fs.readFileSync(csproj).toString();
	prop = prop
		.replace(/<AssemblyVersion>(.*)<\/AssemblyVersion>/, `<AssemblyVersion>${v}</AssemblyVersion>`)
		.replace(/<FileVersion>(.*)<\/FileVersion>/, `<FileVersion>${v}</FileVersion>`)
		.replace(/<InformationalVersion>(.*)<\/InformationalVersion>/, `<InformationalVersion>${v}</InformationalVersion>`);

	fs.writeFileSync(csproj, prop);
}

let manifest = fs.readFileSync('./NextUIPlugin/NextUIPlugin.json').toString();
const dec = JSON.parse(manifest);

dec.AssemblyVersion = v;

const repl = JSON.stringify(dec, null, 4)
.replace('    ', '\t');

fs.writeFileSync('./NextUIPlugin/NextUIPlugin.json', JSON.stringify(dec, null, 4));

// const execSync = require('child_process').execSync;

// console.log(execSync(`git add Properties/AssemblyInfo.cs`, { encoding: 'utf-8' }));
// console.log(execSync(`git add NextUIPlugin/NextUIPlugin.json`, { encoding: 'utf-8' }));
// console.log(execSync(`git commit -m v${v}`, { encoding: 'utf-8' }));
// console.log(execSync(`git tag -a v${v} -m v${v}`, { encoding: 'utf-8' }));