const express = require("express");
const c = require("../controllers/notification.controller");
const { auth } = require("../middleware/auth");
const r = express.Router();
r.get("/", auth(["USER","AGENT","ADMIN"]), c.list);
module.exports = { notificationRouter: r };
