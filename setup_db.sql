-- Tables
CREATE TABLE IF NOT EXISTS public.profiles (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    full_name TEXT NOT NULL DEFAULT '',
    role TEXT NOT NULL DEFAULT 'Technician' CHECK (role IN ('Admin', 'Technician', 'Maintenance')),
    is_active BOOLEAN NOT NULL DEFAULT true,
    must_change_password BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS public.departments (id SERIAL PRIMARY KEY, code TEXT NOT NULL UNIQUE, name TEXT NOT NULL DEFAULT '', description TEXT);
CREATE TABLE IF NOT EXISTS public.machines (id SERIAL PRIMARY KEY, tag_id TEXT NOT NULL UNIQUE, name TEXT NOT NULL DEFAULT '', department TEXT NOT NULL DEFAULT '', status TEXT NOT NULL DEFAULT 'Running' CHECK (status IN ('Running','Broken','InMaintenance','Paused','Removed')), installation_date TIMESTAMPTZ NOT NULL DEFAULT now(), exit_date TIMESTAMPTZ, notes TEXT, last_updated TIMESTAMPTZ NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS public.scan_events (id SERIAL PRIMARY KEY, tag_id TEXT NOT NULL DEFAULT '', machine_id INTEGER REFERENCES public.machines(id), user_id UUID REFERENCES auth.users(id), event_type TEXT NOT NULL DEFAULT 'Scan', timestamp TIMESTAMPTZ NOT NULL DEFAULT now(), notes TEXT);
CREATE TABLE IF NOT EXISTS public.maintenance_sessions (id SERIAL PRIMARY KEY, machine_id INTEGER NOT NULL REFERENCES public.machines(id), technician_id UUID REFERENCES auth.users(id), started_at TIMESTAMPTZ NOT NULL DEFAULT now(), ended_at TIMESTAMPTZ, duration_minutes DOUBLE PRECISION, notes TEXT);

-- RLS
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.machines ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.scan_events ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.maintenance_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.departments ENABLE ROW LEVEL SECURITY;
CREATE POLICY p1 ON public.profiles FOR SELECT USING (auth.role() = 'authenticated');
CREATE POLICY p2 ON public.profiles FOR UPDATE USING (auth.uid() = id);
CREATE POLICY p3 ON public.machines FOR ALL USING (auth.role() = 'authenticated');
CREATE POLICY p4 ON public.scan_events FOR ALL USING (auth.role() = 'authenticated');
CREATE POLICY p5 ON public.maintenance_sessions FOR ALL USING (auth.role() = 'authenticated');
CREATE POLICY p6 ON public.departments FOR SELECT USING (auth.role() = 'authenticated');

-- Données
INSERT INTO public.departments (code, name, description) VALUES ('LTN1','LEONI Tunisie 1','Site principal'),('LTN2','LEONI Tunisie 2','Site secondaire'),('LTN3','LEONI Tunisie 3','Site tertiaire');
INSERT INTO public.machines (tag_id,name,department,status) VALUES ('E200001','Machine Coupe A1','LTN1','Running'),('E200002','Machine Sertissage B2','LTN1','Running'),('E200003','Machine Assemblage C3','LTN2','Running'),('E200004','Presse Hydraulique D4','LTN2','Broken'),('E200005','Robot Soudure E5','LTN3','Running');
