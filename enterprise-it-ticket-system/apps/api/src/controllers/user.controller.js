const { pool } = require("../db/pool");
async function listUsers(_req,res,next){try{const {rows}=await pool.query("select id,name,email,role,created_at from users order by created_at desc");res.json(rows);}catch(e){next(e);}}
module.exports={listUsers};
