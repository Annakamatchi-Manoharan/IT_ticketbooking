function notFound(req, res) { res.status(404).json({ message: `Route not found: ${req.method} ${req.originalUrl}` }); }
function errorHandler(err, req, res, _next) { res.status(err.statusCode || 500).json({ message: err.message || "Internal server error" }); }
module.exports = { notFound, errorHandler };
