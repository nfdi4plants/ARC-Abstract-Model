module TranspileTasks

open BlackFox.Fake

// Include each library's native staging and declaration/representation checks.
let transpileTS =
    BuildTask.createEmpty "TranspileTS"
        [BaseModelTasks.buildBaseModelJS; PolyglotSQLiteTasks.buildPolyglotSQLiteJS]

let transpilePy =
    BuildTask.createEmpty "TranspilePy"
        [BaseModelTasks.buildBaseModelPy; PolyglotSQLiteTasks.buildPolyglotSQLitePy]
