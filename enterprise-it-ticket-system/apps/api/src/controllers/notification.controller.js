const { getNotifications } = require("../services/notification.service");
async function list(req,res,next){try{res.json(await getNotifications(req.user.sub));}catch(e){next(e);}}
module.exports={list};
