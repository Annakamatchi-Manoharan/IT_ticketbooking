const { pool } = require("../db/pool");
async function audit(action, actorId, metadata = {}) { await pool.query("insert into audit_logs(action, actor_id, metadata) values($1,$2,$3)",[action, actorId || null, metadata]); }
module.exports = { audit };
