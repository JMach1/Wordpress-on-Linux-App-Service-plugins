const defaults = require('@wordpress/scripts/config/webpack.config');

module.exports = {
    ...defaults,
    externals: {
        react: 'React',
        'react-dom': 'ReactDOM',
    },
    entry: {
        ...defaults.entry,
        admin: ['./src/admin/index.js'],
        apisList: ['./src/apisList/index.js'],
    },
//    output: {
//        ...defaults.output,
//        path: __dirname + "/dist",
//    },
};
