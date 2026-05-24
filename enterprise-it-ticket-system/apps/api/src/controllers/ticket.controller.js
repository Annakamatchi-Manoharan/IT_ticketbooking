const svc = require("../services/ticket.service");
const { ticketSchema, commentSchema } = require("../validators/schemas");
const { audit } = require("../utils/audit");
async function list(req,res,next){try{res.json(await svc.listTickets(req.query,req.user));}catch(e){next(e);}}
async function create(req,res,next){try{const data=ticketSchema.parse(req.body);const ticket=await svc.createTicket(data,req.user.sub,req.file?.path);await audit("TICKET_CREATE",req.user.sub,{ticketId:ticket.id});res.status(201).json(ticket);}catch(e){next(e);}}
async function update(req,res,next){try{const ticket=await svc.updateTicket(req.params.id,req.body);await audit("TICKET_UPDATE",req.user.sub,{ticketId:req.params.id});res.json(ticket);}catch(e){next(e);}}
async function remove(req,res,next){try{await svc.deleteTicket(req.params.id);await audit("TICKET_DELETE",req.user.sub,{ticketId:req.params.id});res.status(204).send();}catch(e){next(e);}}
async function comment(req,res,next){try{const data=commentSchema.parse(req.body);const row=await svc.addComment(req.params.id,req.user.sub,data.message);res.status(201).json(row);}catch(e){next(e);}}
module.exports={list,create,update,remove,comment};
