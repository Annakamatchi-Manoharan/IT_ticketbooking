const express = require("express");
const c = require("../controllers/user.controller");
const { auth } = require("../middleware/auth");
const r = express.Router();
r.get("/", auth(["ADMIN"]), c.listUsers);
module.exports = { userRouter: r };
