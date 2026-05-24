const express = require("express");
const c = require("../controllers/auth.controller");
const r = express.Router();
r.post("/signup", c.signup);
r.post("/login", c.login);
r.post("/password-reset", c.passwordReset);
module.exports = { authRouter: r };
