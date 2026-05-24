-- MySQL 10.x script for chatbot module (parallel to EF migrations on SQL Server)
-- Run on your IT ticket database if using MySQL as specified in project documentation.

CREATE TABLE IF NOT EXISTS support_faqs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    category VARCHAR(120) NOT NULL,
    issue_title VARCHAR(200) NOT NULL,
    steps_json JSON NOT NULL,
    problem_type INT NOT NULL COMMENT '0=Network,1=Software,2=Hardware',
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_support_faqs_category (category)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS user_chat_history (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    searched_issue VARCHAR(200) NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_chat_history_user_created (user_id, created_at),
    CONSTRAINT fk_user_chat_history_user
        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS engineer_chat_history (
    id INT AUTO_INCREMENT PRIMARY KEY,
    engineer_id INT NOT NULL,
    query VARCHAR(2000) NOT NULL,
    predicted_category VARCHAR(120) NULL,
    bot_response JSON NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_engineer_chat_history_eng_created (engineer_id, created_at),
    CONSTRAINT fk_engineer_chat_history_engineer
        FOREIGN KEY (engineer_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
