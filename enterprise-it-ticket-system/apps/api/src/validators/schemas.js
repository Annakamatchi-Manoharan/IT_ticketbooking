const z = require("zod");
const registerSchema = z.object({ name: z.string().min(2).max(80), email: z.string().email(), password: z.string().min(8), role: z.enum(["USER","AGENT","ADMIN"]).default("USER") });
const loginSchema = z.object({ email: z.string().email(), password: z.string().min(8) });
const ticketSchema = z.object({ title: z.string().min(5).max(120), description: z.string().min(10).max(5000), category: z.enum(["HARDWARE","SOFTWARE","NETWORK","SECURITY","ACCESS_REQUEST","OTHER"]), priority: z.enum(["LOW","MEDIUM","HIGH","CRITICAL"]).default("MEDIUM"), assignedAgentId: z.string().uuid().optional(), status: z.enum(["OPEN","IN_PROGRESS","PENDING","RESOLVED","CLOSED"]).optional() });
const commentSchema = z.object({ message: z.string().min(1).max(1500) });
module.exports = { registerSchema, loginSchema, ticketSchema, commentSchema };
