const express = require("express");
const c = require("../controllers/dashboard.controller");
const { auth } = require("../middleware/auth");
const r = express.Router();
r.get("/admin", auth(["ADMIN"]), c.adminMetrics);
module.exports = { dashboardRouter: r };
