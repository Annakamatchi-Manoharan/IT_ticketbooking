const multer = require("multer");
const storage = multer.diskStorage({ destination: "uploads/", filename: (_req, file, cb) => cb(null, `${Date.now()}-${file.originalname.replace(/[^a-zA-Z0-9.-]/g, "_")}`) });
const allowed = ["image/png", "image/jpeg", "application/pdf", "text/plain"];
const upload = multer({ storage, limits: { fileSize: 5 * 1024 * 1024 }, fileFilter: (_req, file, cb) => { if (!allowed.includes(file.mimetype)) return cb(new Error("Invalid file type")); cb(null, true); } });
module.exports = { upload };
