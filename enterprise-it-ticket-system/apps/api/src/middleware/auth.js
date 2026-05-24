const jwt = require("jsonwebtoken");

function auth(requiredRoles = []) {
  return (req, res, next) => {
    const token = req.headers.authorization?.replace("Bearer ", "");
    if (!token) return res.status(401).json({ message: "Unauthorized" });
    try {
      const decoded = jwt.verify(token, process.env.JWT_ACCESS_SECRET);
      req.user = decoded;
      if (requiredRoles.length && !requiredRoles.includes(decoded.role)) {
        return res.status(403).json({ message: "Forbidden" });
      }
      next();
    } catch {
      return res.status(401).json({ message: "Invalid token" });
    }
  };
}

module.exports = { auth };
