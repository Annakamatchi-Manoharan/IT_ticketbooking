function validate(schema) { return (req,res,next)=>{ const payload={...req.body,...req.params,...req.query}; const result=schema.safeParse(payload); if(!result.success){return res.status(400).json({message:"Validation failed",issues:result.error.issues});} next();}; }
module.exports = { validate };
