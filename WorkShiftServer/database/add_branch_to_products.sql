-- Scope products per branch.
-- Up:
-- Test data cleanup (authorized): products currently holds a single test row.
DELETE FROM products;

ALTER TABLE products
    ADD COLUMN branch_id integer NOT NULL;

ALTER TABLE products
    DROP CONSTRAINT products_product_code_key;

ALTER TABLE products
    ADD CONSTRAINT uq_products_branch_code UNIQUE (branch_id, product_code);

ALTER TABLE products
    ADD CONSTRAINT products_branch_id_fkey
    FOREIGN KEY (branch_id) REFERENCES branches (branch_id) ON DELETE RESTRICT;

CREATE INDEX ix_products_branch_id ON products (branch_id);

-- Down:
-- ALTER TABLE products DROP CONSTRAINT products_branch_id_fkey;
-- DROP INDEX ix_products_branch_id;
-- ALTER TABLE products DROP CONSTRAINT uq_products_branch_code;
-- ALTER TABLE products ADD CONSTRAINT products_product_code_key UNIQUE (product_code);
-- ALTER TABLE products DROP COLUMN branch_id;
