const data = require('./result.json');
const fs = require('fs');

const cleanStaticIdSuffix = (id) => {
  for (let i = id.length - 1; i >= 0; i--) {
    const isNumber = (id[i] ^ '0') !== 0 || id[i] === '0';
    const isUnderscore = (id[i] === '_');

    if (!isNumber && !isUnderscore) {
      return id.substring(0, i + 1);
    }
  }

  return id;
};

const result = data.reduce((acc, curr) => {
    const type = cleanStaticIdSuffix(curr.StaticId.split('.').pop());

    if (!acc.includes(type)) {
        acc.push(type);
    }

    return acc;
}, [])

fs.writeFileSync('types.json', JSON.stringify(result));
