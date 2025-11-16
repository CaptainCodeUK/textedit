const manifestFileName = process.argv[2];
// @ts-ignore
const manifestFile = require('../electron.manifest.json');
const dasherize = require('dasherize');
const fs = require('fs');

const builderConfiguration = { ...manifestFile.build };
if(process.argv.length > 3) {
    builderConfiguration.buildVersion = process.argv[3];
}
if(builderConfiguration.hasOwnProperty('buildVersion')) {
    // @ts-ignore
    const packageJson = { name: dasherize(manifestFile.name || 'electron-net'), author: manifestFile.author || '', version: builderConfiguration.buildVersion, description: manifestFile.description || '' };
    fs.writeFile('./package.json', JSON.stringify(packageJson), (error) => {
        if(error) {
            console.log(error.message);
        }
    });
}

const builderConfigurationString = JSON.stringify(builderConfiguration);
const outDir = './obj/Host/bin';
fs.mkdir(outDir, { recursive: true }, (err) => {
    if(err) throw err;
    fs.writeFile(outDir + '/electron-builder.json', builderConfigurationString, (error) => {
        if(error) {
            console.log(error.message);
        }
    });
    const manifestContent = JSON.stringify(manifestFile);
    fs.writeFile(outDir + '/electron.manifest.json', manifestContent, (error) => {
        if(error) {
            console.log(error.message);
        }
    });
});
