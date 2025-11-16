const manifestFileName = process.argv[2];
// @ts-ignore
const manifestFile = require('../electron.manifest.json');
// Small dasherize helper – avoids installing a module in CI for this simple transform.
function dasherize(input) {
    if (!input) return 'electron-net';
    return String(input)
        .trim()
        .toLowerCase()
        .replace(/[^a-z0-9\s-]/g, '')
        .replace(/\s+/g, '-')
        .replace(/-+/g, '-');
}
const fs = require('fs');
const path = require('path');

const builderConfiguration = { ...manifestFile.build };
const projectRoot = path.resolve(__dirname, '..');
if(process.argv.length > 3) {
    builderConfiguration.buildVersion = process.argv[3];
}
if(builderConfiguration.hasOwnProperty('buildVersion')) {
    // @ts-ignore
    const packageJson = { name: dasherize(manifestFile.name || 'electron-net'), author: manifestFile.author || '', version: builderConfiguration.buildVersion, description: manifestFile.description || '' };
    // Write package.json next to the host output so npm can run there if required
    const hostPackagePath = path.join(projectRoot, 'obj', 'Host', 'package.json');
    try {
        fs.writeFileSync(hostPackagePath, JSON.stringify(packageJson));
    } catch (error) {
        console.log(error.message);
    }
}

const builderConfigurationString = JSON.stringify(builderConfiguration);
const outDir = path.join(projectRoot, 'obj', 'Host', 'bin');
fs.mkdirSync(outDir, { recursive: true });
fs.writeFileSync(path.join(outDir, 'electron-builder.json'), builderConfigurationString);
fs.writeFileSync(path.join(outDir, 'electron.manifest.json'), JSON.stringify(manifestFile));
console.log('Wrote electron-builder.json to', outDir);
