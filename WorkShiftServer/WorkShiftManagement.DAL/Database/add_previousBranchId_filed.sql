ALTER TABLE public.users
ADD COLUMN previous_branch_id integer
REFERENCES public.branches(branch_id)
ON DELETE RESTRICT;