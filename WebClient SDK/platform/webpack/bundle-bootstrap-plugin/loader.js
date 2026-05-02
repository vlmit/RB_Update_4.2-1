const path = require('path');
const { Operation } = require('./operation');

module.exports = function modifyModuleSourceLoader(source) {
  const options = this.getOptions();

  const cleanPath = options.moduleRequest.split('?')[0];
  const fileName = path.basename(cleanPath);

  return options.operations.reduce((sourceText, serializableOp) => {
    const operation = Operation.fillConstants(Operation.fromSerializable(serializableOp), {
      ...options.constants,
      FILE_PATH: cleanPath,
      FILE_NAME: fileName
    });

    return Operation.apply(sourceText, operation);
  }, source);
};
