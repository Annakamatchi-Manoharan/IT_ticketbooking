const svc = require("../services/dashboard.service");
async function adminMetrics(req,res,next){try{res.json(await svc.getAdminDashboard());}catch(e){next(e);}}
module.exports={adminMetrics};
