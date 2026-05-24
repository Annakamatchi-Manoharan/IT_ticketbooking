const authService = require("../services/auth.service");
const { registerSchema, loginSchema } = require("../validators/schemas");
const { audit } = require("../utils/audit");
async function signup(req,res,next){try{const data=registerSchema.parse(req.body);const user=await authService.signup(data);await audit("AUTH_SIGNUP",user.id,{email:user.email});res.status(201).json(user);}catch(e){next(e);}}
async function login(req,res,next){try{const data=loginSchema.parse(req.body);const payload=await authService.login(data.email,data.password);if(!payload)return res.status(401).json({message:"Invalid credentials"});await audit("AUTH_LOGIN",payload.user.id,{});res.json(payload);}catch(e){next(e);}}
async function passwordReset(_req,res){res.json({message:"Password reset workflow triggered."});}
module.exports={signup,login,passwordReset};
