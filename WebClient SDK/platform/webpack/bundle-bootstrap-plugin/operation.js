class ConcatOperation {
  constructor(type, value) {
    this.type = type;
    this.value = value;
  }

  getSerializableProperties() {
    return ['type', 'value'];
  }

  getTextProperties() {
    return ['value'];
  }

  static getAllowedTypes() {
    return ['start', 'end'];
  }
}

function isSerializableOfOperation(serializable, operation) {
  return serializable.operationName === operation.name;
}

function throwUnknownOperationType(op, opType) {
  const allowedTypes = ConcatOperation.getAllowedTypes();

  throw new Error(
    `Incorrect operation type '${opType}' for ${
      op.constructor.name
    }. Allowed types: '${allowedTypes.join("', '")}'.`
  );
}

class Operation {
  static makeSerializable(op) {
    const propertyValues = op.getSerializableProperties().reduce((acc, val) => {
      return {
        ...acc,
        [val]: op[val]
      };
    }, {});

    return {
      operationName: op.constructor.name,
      ...propertyValues
    };
  }

  static fromSerializable(serializable) {
    if (isSerializableOfOperation(serializable, ConcatOperation)) {
      const { type, value } = serializable;

      return new ConcatOperation(type, value);
    }

    throw new Error('Incorrect serializable provided: ' + JSON.stringify(serializable));
  }

  static fillConstants(operation, constants) {
    const filledTextProps = operation.getTextProperties().reduce((acc, propName) => {
      let propValue = operation[propName];

      Object.keys(constants).forEach(constant => {
        propValue = propValue.replace(
          new RegExp(`\\$${constant}`, 'g'),
          String(constants[constant])
        );
      });

      return {
        ...acc,
        [propName]: propValue
      };
    }, {});

    const mergedObject = {
      ...Operation.makeSerializable(operation),
      ...filledTextProps
    };

    return Operation.fromSerializable(mergedObject);
  }

  static apply(src, operation) {
    if (operation instanceof ConcatOperation) {
      switch (operation.type) {
        case 'start':
          return operation.value + src;

        case 'end':
          return src + operation.value;

        default:
          throwUnknownOperationType(operation, operation.type);
      }
    }

    throw new Error('Unknown operation instance: ' + operation.constructor.name);
  }
}

module.exports = {
  Operation,
  ConcatOperation
};
